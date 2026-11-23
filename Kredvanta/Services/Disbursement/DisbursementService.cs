using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Kredvanta.Data;
using Kredvanta.Models.Loans;
using Kredvanta.Models.Repayments;
using Kredvanta.Services.Audit;
using Kredvanta.Services.Contracts;
using Kredvanta.Services.Credit;
using Kredvanta.Services.Workflow;
using Microsoft.EntityFrameworkCore;
using static Kredvanta.Models.Enums.LendingEnums;

namespace Kredvanta.Services.Disbursement;

public class DisburseRequest
{
    public Guid ApplicationId { get; set; }

    public DisbursementMethod Method { get; set; }

    [StringLength(100)]
    public string Reference { get; set; } = "";

    [Required, StringLength(100)]
    public string RecipientAccount { get; set; } = "";

    [Range(typeof(decimal), "0.01", "9999999999999999.99")]
    public decimal AmountPaid { get; set; }

    [Required, StringLength(64, MinimumLength = 32)]
    public string IdempotencyKey { get; set; } = "";
}

public record DisburseResult(bool Succeeded, string? Error = null, string? Message = null)
{
    public static DisburseResult Fail(string e) => new(false, e);
    public static DisburseResult Ok(string m) => new(true, null, m);
}

public interface IDisbursementService
{
    Task<DisburseResult> DisburseAsync(DisburseRequest r, string financeUserId, CancellationToken ct = default);
}

public class DisbursementService : IDisbursementService
{
    private readonly ApplicationDbContext _db;
    private readonly IApplicationWorkflowService _workflow;
    private readonly IAuditService _audit;

    public DisbursementService(ApplicationDbContext db, IApplicationWorkflowService workflow, IAuditService audit)
    { _db = db; _workflow = workflow; _audit = audit; }

    public async Task<DisburseResult> DisburseAsync(DisburseRequest r, string financeUserId,
        CancellationToken ct = default)
    {
        var idempotencyKey = r.IdempotencyKey?.Trim();
        if (string.IsNullOrWhiteSpace(idempotencyKey) || idempotencyKey.Length is < 32 or > 64)
            return DisburseResult.Fail("Reload the page and try again.");
        if (!Enum.IsDefined(r.Method))
            return DisburseResult.Fail("Select a valid disbursement method.");
        if (r.AmountPaid <= 0)
            return DisburseResult.Fail("The amount paid must be greater than zero.");

        var priorRequest = await FindPriorRequestAsync(idempotencyKey, r.ApplicationId, financeUserId, ct);
        if (priorRequest is not null) return priorRequest;

        var app = await _db.LoanApplications.Include(a => a.Borrower)
            .FirstOrDefaultAsync(a => a.Id == r.ApplicationId, ct);
        if (app is null) return DisburseResult.Fail("Application not found.");
        if (app.Status != LoanApplicationStatus.ReadyForDisbursement)
            return DisburseResult.Fail("This application is not ready for disbursement.");
        if (app.Borrower.ApplicationUserId == financeUserId)
            return DisburseResult.Fail("You cannot disburse your own loan.");
        if (await _db.Loans.AnyAsync(l => l.LoanApplicationId == app.Id, ct))
            return DisburseResult.Fail("A loan already exists for this application.");

        var offer = await _db.LoanOffers.Where(o => o.LoanApplicationId == app.Id && o.Status == OfferStatus.Accepted)
            .OrderByDescending(o => o.Version).FirstOrDefaultAsync(ct);
        if (offer is null) return DisburseResult.Fail("There is no accepted offer.");

        var agreement = await _db.LoanAgreements
            .FirstOrDefaultAsync(a => a.LoanOfferId == offer.Id && a.Status == AgreementStatus.Signed, ct);
        if (agreement is null) return DisburseResult.Fail("The agreement has not been signed.");
        if (OfferService.Hash(agreement.Text) != agreement.Sha256Hash)
            return DisburseResult.Fail("The agreement failed its integrity check. Do not pay out.");
        if (!await _db.AgreementSignatures.AnyAsync(s =>
                s.LoanAgreementId == agreement.Id && s.AgreementHash == agreement.Sha256Hash, ct))
            return DisburseResult.Fail("The agreement signature could not be verified. Do not pay out.");

        var expected = offer.Principal - offer.ProcessingFee;
        if (r.AmountPaid != expected)
            return DisburseResult.Fail($"The amount must be exactly {offer.CurrencyCode} {expected:N2} " +
                                       "(approved principal minus the processing fee).");

        var reference = r.Reference?.Trim() ?? "";
        if (reference.Length > 100)
            return DisburseResult.Fail("The transaction reference cannot exceed 100 characters.");
        if (r.Method != DisbursementMethod.Cash && reference.Length == 0)
            return DisburseResult.Fail("Enter the provider's transaction reference.");
        if (reference.Length > 0 && await _db.LoanDisbursements.AnyAsync(d => d.TransactionReference == reference, ct))
            return DisburseResult.Fail("This transaction reference has already been recorded.");
        var recipientAccount = r.RecipientAccount?.Trim() ?? "";
        if (recipientAccount.Length == 0)
            return DisburseResult.Fail("Enter where the money was sent.");
        if (recipientAccount.Length > 100)
            return DisburseResult.Fail("The recipient details cannot exceed 100 characters.");

        List<ScheduleLine>? schedule;
        try
        {
            schedule = JsonSerializer.Deserialize<List<ScheduleLine>>(offer.ScheduleJson);
        }
        catch (JsonException)
        {
            return DisburseResult.Fail("The offer schedule is invalid. Do not pay out.");
        }

        if (schedule is null || schedule.Count != offer.TermPeriods || schedule.Any(line => line is null) ||
            schedule.Where((line, index) => line.Number != index + 1 || line.Principal < 0 || line.Interest < 0 ||
                                           line.Total != line.Principal + line.Interest).Any() ||
            schedule.Sum(line => line.Principal) != offer.Principal ||
            schedule.Sum(line => line.Interest) != offer.TotalInterest ||
            schedule.Sum(line => line.Total) != offer.TotalRepayable)
            return DisburseResult.Fail("The offer schedule is inconsistent. Do not pay out.");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var loan = new Loan
        {
            LoanNumber = $"LN-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}",
            LoanApplicationId = app.Id,
            BorrowerId = app.BorrowerId,
            LoanOfferId = offer.Id,
            ProductNameSnapshot = offer.ProductNameSnapshot,
            ProductCodeSnapshot = offer.ProductCodeSnapshot,
            PrincipalAmount = offer.Principal,
            AnnualInterestRate = offer.AnnualInterestRate,
            InterestMethod = offer.InterestMethod,
            RepaymentFrequency = offer.RepaymentFrequency,
            NumberOfInstallments = offer.TermPeriods,
            StartDate = today,
            TotalInterest = offer.TotalInterest,
            ProcessingFee = offer.ProcessingFee,
            TotalRepayable = offer.TotalRepayable,
            OutstandingBalance = offer.TotalRepayable,
            CurrencyCode = offer.CurrencyCode,
            Status = LoanStatus.Active,
            DisbursementDate = today,
            MaturityDate = offer.MaturityDate
        };
        _db.Loans.Add(loan);

        foreach (var l in schedule)
            _db.RepaymentSchedules.Add(new RepaymentSchedule
            {
                LoanId = loan.Id,
                InstallmentNumber = l.Number,
                DueDate = l.DueDate,
                PrincipalDue = l.Principal,
                InterestDue = l.Interest,
                FeesDue = 0m,
                TotalDue = l.Total,
                AmountPaid = 0m,
                Status = ScheduleStatus.Pending
            });

        var disbursement = new LoanDisbursement
        {
            LoanId = loan.Id,
            Amount = r.AmountPaid,
            Method = r.Method,
            TransactionReference = reference.Length == 0 ? null : reference,
            RecipientAccount = recipientAccount,
            IdempotencyKey = idempotencyKey,
            ProcessedByUserId = financeUserId,
            Status = DisbursementStatus.Successful,
            DisbursedAt = DateTime.UtcNow
        };
        _db.LoanDisbursements.Add(disbursement);

        _audit.Record("LoanDisbursed", nameof(Loan), loan.Id.ToString(), null, new
        {
            loan.LoanNumber,
            r.Method,
            Amount = r.AmountPaid,
            Reference = reference,
            Principal = offer.Principal,
            Fee = offer.ProcessingFee,
            Offer = $"v{offer.Version}"
        });

        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            // Saves the loan, schedule, disbursement and audit row together with the status change.
            var t = await _workflow.TransitionAsync(app.Id, LoanApplicationStatus.Disbursed, financeUserId,
                $"Disbursed as {loan.LoanNumber}.", ct);
            if (!t.Succeeded)
            {
                await tx.RollbackAsync(ct);
                Discard();
                return DisburseResult.Fail(t.Error ?? "The disbursement could not be recorded.");
            }
            await tx.CommitAsync(ct);
        }
        catch (DbUpdateException)
        {
            await tx.RollbackAsync(ct);
            Discard();
            return DisburseResult.Fail(
                "This payout conflicts with one already recorded. Check the list before trying again.");
        }

        return DisburseResult.Ok($"Loan {loan.LoanNumber} created and recorded as disbursed.");
    }

    private async Task<DisburseResult?> FindPriorRequestAsync(string key, Guid applicationId, string userId,
        CancellationToken ct)
    {
        var prior = await _db.LoanDisbursements.AsNoTracking()
            .Where(d => d.IdempotencyKey == key)
            .Select(d => new { d.Loan.LoanApplicationId, d.ProcessedByUserId })
            .FirstOrDefaultAsync(ct);

        if (prior is null) return null;
        return prior.LoanApplicationId == applicationId && prior.ProcessedByUserId == userId
            ? DisburseResult.Ok("This payout was already recorded.")
            : DisburseResult.Fail("This request key has already been used.");
    }

    private void Discard()
    {
        foreach (var e in _db.ChangeTracker.Entries()
                     .Where(e => e.State == EntityState.Added).ToList())
            e.State = EntityState.Detached;
    }
}