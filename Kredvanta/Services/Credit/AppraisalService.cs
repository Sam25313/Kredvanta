using Kredvanta.Data;
using Kredvanta.Models.Applications;
using Kredvanta.Models.Enums;
using Kredvanta.Models.Verification;
using Kredvanta.Services.Audit;
using Microsoft.EntityFrameworkCore;
using static Kredvanta.Models.Enums.LendingEnums;


namespace Kredvanta.Services.Credit;

public class AppraisalService : IAppraisalService
{
    private readonly ApplicationDbContext _db;
    private readonly IAuditService _audit;

    public AppraisalService(ApplicationDbContext db, IAuditService audit)
    {
        _db = db; _audit = audit;
    }

    public async Task<AppraisalPreview> PreviewAsync(Guid applicationId, AppraisalInput input,
        CancellationToken ct = default)
        => (await ComputeAsync(applicationId, input, ct)).Preview;

    public async Task<AppraisalOutcome> AssessAsync(Guid applicationId, AppraisalInput input,
        string analystId, CancellationToken ct = default)
    {
        var c = await ComputeAsync(applicationId, input, ct);
        var p = c.Preview;

        if (!p.Ready || c.App is null || c.Policy is null || p.Affordability is null)
            return AppraisalOutcome.Fail(p.BlockReason ?? "The assessment cannot be completed.");

        if (c.App.Status != LoanApplicationStatus.UnderReview)
            return AppraisalOutcome.Fail("The application must be under review to be assessed.");

        if (c.App.Borrower.ApplicationUserId == analystId)
            return AppraisalOutcome.Fail("You cannot assess your own application.");

        if (input.ExistingMonthlyLoanPayments < 0 || input.OtherMonthlyObligations < 0 || input.MonthlyExpenses < 0)
            return AppraisalOutcome.Fail("Amounts cannot be negative.");

        if (p.Flags.Count > 0 && string.IsNullOrWhiteSpace(input.Notes))
            return AppraisalOutcome.Fail("Risk flags were raised. Explain how you considered them in the notes.");

        CreditRecommendation recommendation;
        if (!p.Affordability.Passes)
            recommendation = CreditRecommendation.DoNotRecommend;
        else if (input.RecommendWithConditions)
        {
            if (string.IsNullOrWhiteSpace(input.Notes))
                return AppraisalOutcome.Fail("State the conditions in the notes.");
            recommendation = CreditRecommendation.RecommendWithConditions;
        }
        else recommendation = CreditRecommendation.Recommend;

        // Retire earlier assessments: exactly one current assessment per application.
        var earlier = await _db.CreditAssessments
            .Where(a => a.LoanApplicationId == applicationId && a.IsCurrent).ToListAsync(ct);
        foreach (var e in earlier) e.IsCurrent = false;

        var a2 = p.Affordability;
        var assessment = new CreditAssessment
        {
            LoanApplicationId = applicationId,
            MonthlyIncome = p.VerifiedNetIncome!.Value,
            MonthlyExpenses = input.MonthlyExpenses,
            ExistingMonthlyLoanPayments = input.ExistingMonthlyLoanPayments,
            OtherMonthlyObligations = input.OtherMonthlyObligations,
            DisposableIncome = a2.DisposableBefore,
            ProposedMonthlyInstallment = p.MonthlyEquivalent,
            DebtServiceRatio = a2.DebtServiceRatio,
            AssessedByUserId = analystId,

            CreditPolicyVersionId = c.Policy.Id,
            CalculationVersion = c.Policy.CalculationVersion,
            IsCurrent = true,
            Recommendation = recommendation,
            RiskFlags = p.Flags.Count == 0 ? null : string.Join("; ", p.Flags),
            Notes = input.Notes?.Trim(),
            AssessedAmount = c.App.RequestedAmount,
            AssessedTermPeriods = c.App.RequestedTermPeriods,
            AssessedAt = DateTime.UtcNow
        };
        _db.CreditAssessments.Add(assessment);

        _audit.Record("CreditAssessmentRecorded", nameof(LoanApplication), applicationId.ToString(), null, new
        {
            Policy = $"v{c.Policy.VersionNumber}",
            p.VerifiedNetIncome,
            Instalment = p.MonthlyEquivalent,
            Ratio = a2.DebtServiceRatio,
            Passed = a2.Passes,
            Recommendation = recommendation.ToString(),
            Flags = p.Flags
        });

        await _db.SaveChangesAsync(ct);
        return new AppraisalOutcome(true, null, recommendation);
    }

    // ---------- shared computation ----------

    private record Computation(AppraisalPreview Preview, LoanApplication? App,
        Kredvanta.Models.Credit.CreditPolicyVersion? Policy);

    private async Task<Computation> ComputeAsync(Guid applicationId, AppraisalInput input, CancellationToken ct)
    {
        var preview = new AppraisalPreview();

        var app = await _db.LoanApplications.AsNoTracking()
            .Include(a => a.Borrower).Include(a => a.LoanProduct)
            .FirstOrDefaultAsync(a => a.Id == applicationId, ct);
        if (app is null) { preview.BlockReason = "Application not found."; return new(preview, null, null); }

        var policy = await _db.CreditPolicyVersions.AsNoTracking().FirstOrDefaultAsync(p => p.IsActive, ct);
        if (policy is null)
        {
            preview.BlockReason = "No approved credit policy is active. Ask a credit manager to activate one.";
            return new(preview, app, null);
        }
        preview.PolicyName = policy.Name;
        preview.PolicyVersion = policy.VersionNumber;

        var b = app.Borrower;
        preview.DeclaredNetIncome = b.NetMonthlySalary;

        // Income only from an independently verified employer confirmation.
        var ver = await _db.EmploymentVerifications.AsNoTracking()
            .Where(v => v.LoanApplicationId == applicationId)
            .OrderByDescending(v => v.AttemptedAt).FirstOrDefaultAsync(ct);

        if (ver is not { Outcome: EmploymentVerificationOutcome.Verified, ContactIndependentlySourced: true }
            || ver.ConfirmedNetSalary is null or <= 0)
        {
            preview.BlockReason = "Employment must be independently verified before an assessment can be made.";
            return new(preview, app, policy);
        }
        preview.VerifiedNetIncome = ver.ConfirmedNetSalary;

        var product = app.LoanProduct;
        var firstDue = ScheduleCalculator.DueDate(DateOnly.FromDateTime(DateTime.UtcNow),
            product.RepaymentFrequency, 1);
        var schedule = ScheduleCalculator.Calculate(app.RequestedAmount, product.AnnualInterestRate,
            app.RequestedTermPeriods, product.RepaymentFrequency, product.InterestMethod, firstDue);

        preview.Schedule = schedule.Lines.ToList();
        preview.RegularInstalment = schedule.RegularInstalment;
        preview.TotalInterest = schedule.TotalInterest;
        preview.TotalRepayable = schedule.TotalRepayable;
        preview.ProcessingFee = Math.Round(
            app.RequestedAmount * product.ProcessingFeePercentage / 100m + product.ProcessingFeeFixed,
            2, MidpointRounding.AwayFromZero);

        // Normalise to a monthly figure so weekly and bi-weekly loans are comparable.
        preview.MonthlyEquivalent = Math.Round(
            schedule.RegularInstalment * ScheduleCalculator.PeriodsPerYear(product.RepaymentFrequency) / 12m,
            2, MidpointRounding.AwayFromZero);

        preview.Affordability = AffordabilityCalculator.Evaluate(
            new AffordabilityInput(ver.ConfirmedNetSalary.Value, input.ExistingMonthlyLoanPayments,
                input.OtherMonthlyObligations, input.MonthlyExpenses, preview.MonthlyEquivalent),
            new AffordabilityPolicy(policy.MaxDebtServiceRatio, policy.MinNetMonthlyIncome,
                policy.MinDisposableAfterInstalment));

        // Risk flags (they require analyst notes; they do not decide the outcome)
        var maturity = schedule.Lines[^1].DueDate;
        if (b.EmploymentEndDate is { } end && end < maturity)
            preview.Flags.Add($"Contract ends ({end:dd MMM yyyy}) before the loan matures ({maturity:dd MMM yyyy}).");
        if (b.NetMonthlySalary.HasValue && b.NetMonthlySalary != ver.ConfirmedNetSalary)
            preview.Flags.Add("Verified salary differs from the salary declared by the applicant.");
        if (b.EmploymentContractType == EmploymentContractType.Probationary)
            preview.Flags.Add("Applicant is on probation.");

        preview.Ready = true;
        return new(preview, app, policy);
    }
}