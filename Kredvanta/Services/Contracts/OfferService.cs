using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Kredvanta.Data;
using Kredvanta.Models.Applications;
using Kredvanta.Models.Contracts;
using Kredvanta.Models.Identity;
using Kredvanta.Services.Audit;
using Kredvanta.Services.Credit;
using Kredvanta.Services.Workflow;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using static Kredvanta.Models.Enums.LendingEnums;

namespace Kredvanta.Services.Contracts;

public record OfferTerms(decimal Principal, decimal Rate, InterestMethod Method, RepaymentFrequency Frequency,
    int Term, decimal Fee, decimal NetProceeds, decimal TotalInterest, decimal TotalRepayable,
    decimal Instalment, DateOnly FirstDue, DateOnly Maturity, string? Conditions,
    IReadOnlyList<ScheduleLine> Schedule);

public record OfferResult(bool Succeeded, string? Error = null, string? Message = null)
{
    public static OfferResult Fail(string e) => new(false, e);
    public static OfferResult Ok(string m) => new(true, null, m);
}

public interface IOfferService
{
    Task<(OfferTerms? Terms, string? Error)> PreviewAsync(Guid applicationId, DateOnly? firstDue, CancellationToken ct = default);
    Task<OfferResult> IssueAsync(Guid applicationId, DateOnly firstDue, string officerId, CancellationToken ct = default);
    Task<OfferResult> AcceptAsync(Guid applicationId, string userId, CancellationToken ct = default);
    Task<OfferResult> DeclineAsync(Guid applicationId, string? reason, string userId, CancellationToken ct = default);
    Task<OfferResult> SignAsync(Guid applicationId, string password, string userId, string? ip, string? userAgent, CancellationToken ct = default);
    Task<int> ExpireOverdueAsync(CancellationToken ct = default);
}

public class OfferService : IOfferService
{
    private readonly ApplicationDbContext _db;
    private readonly IApplicationWorkflowService _workflow;
    private readonly IAuditService _audit;
    private readonly UserManager<ApplicationUser> _users;
    private readonly IConfiguration _config;
    private readonly IHostEnvironment _env;

    public OfferService(ApplicationDbContext db, IApplicationWorkflowService workflow, IAuditService audit,
        UserManager<ApplicationUser> users, IConfiguration config, IHostEnvironment env)
    { _db = db; _workflow = workflow; _audit = audit; _users = users; _config = config; _env = env; }

    // ---------- Preview / terms ----------

    public async Task<(OfferTerms? Terms, string? Error)> PreviewAsync(Guid applicationId, DateOnly? firstDue,
        CancellationToken ct = default)
    {
        var app = await _db.LoanApplications.AsNoTracking().Include(a => a.LoanProduct)
            .FirstOrDefaultAsync(a => a.Id == applicationId, ct);
        if (app is null) return (null, "Application not found.");
        if (app.Status != LoanApplicationStatus.Approved) return (null, "Offers can only be issued for approved applications.");

        var approvals = await _db.LoanApprovals.AsNoTracking()
            .Where(a => a.LoanApplicationId == applicationId && a.Decision == ApprovalDecision.Approved)
            .OrderBy(a => a.ApprovalLevel).ToListAsync(ct);
        if (approvals.Count == 0) return (null, "There is no recorded approval for this application.");

        var principal = approvals.Last().ApprovedAmount ?? app.RequestedAmount;
        var p = app.LoanProduct;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var first = firstDue ?? ScheduleCalculator.DueDate(today, p.RepaymentFrequency, 1);
        if (first <= today) return (null, "The first instalment date must be in the future.");

        var schedule = ScheduleCalculator.Calculate(principal, p.AnnualInterestRate, app.RequestedTermPeriods,
            p.RepaymentFrequency, p.InterestMethod, first);

        var fee = Math.Round(principal * p.ProcessingFeePercentage / 100m + p.ProcessingFeeFixed, 2,
            MidpointRounding.AwayFromZero);
        var conditions = string.Join("; ", approvals.Where(a => !string.IsNullOrWhiteSpace(a.Conditions))
            .Select(a => a.Conditions!.Trim()).Distinct());

        return (new OfferTerms(principal, p.AnnualInterestRate, p.InterestMethod, p.RepaymentFrequency,
            app.RequestedTermPeriods, fee, principal - fee, schedule.TotalInterest, schedule.TotalRepayable,
            schedule.RegularInstalment, first, schedule.Lines[^1].DueDate,
            conditions.Length == 0 ? null : conditions, schedule.Lines), null);
    }

    // ---------- Issue ----------

    public async Task<OfferResult> IssueAsync(Guid applicationId, DateOnly firstDue, string officerId,
        CancellationToken ct = default)
    {
        var (t, error) = await PreviewAsync(applicationId, firstDue, ct);
        if (t is null) return OfferResult.Fail(error!);

        var app = await _db.LoanApplications.Include(a => a.LoanProduct)
            .FirstAsync(a => a.Id == applicationId, ct);

        var days = _config.GetValue<int?>("Offers:ValidityDays") ?? 7;   // placeholder: confirm with policy
        var version = (await _db.LoanOffers.Where(o => o.LoanApplicationId == applicationId)
            .MaxAsync(o => (int?)o.Version, ct) ?? 0) + 1;

        var offer = new LoanOffer
        {
            LoanApplicationId = applicationId,
            Version = version,
            Status = OfferStatus.Issued,
            IssuedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(days),
            IssuedByUserId = officerId,
            ProductCodeSnapshot = app.LoanProduct.ProductCode,
            ProductNameSnapshot = app.LoanProduct.ProductName,
            Principal = t.Principal,
            AnnualInterestRate = t.Rate,
            InterestMethod = t.Method,
            RepaymentFrequency = t.Frequency,
            TermPeriods = t.Term,
            ProcessingFee = t.Fee,
            TotalInterest = t.TotalInterest,
            TotalRepayable = t.TotalRepayable,
            InstalmentAmount = t.Instalment,
            FirstDueDate = t.FirstDue,
            MaturityDate = t.Maturity,
            Conditions = t.Conditions,
            ScheduleJson = JsonSerializer.Serialize(t.Schedule)
        };
        _db.LoanOffers.Add(offer);

        var tr = await _workflow.TransitionAsync(applicationId, LoanApplicationStatus.OfferIssued, officerId,
            $"Offer version {version} issued.", ct);
        if (!tr.Succeeded)
        {
            _db.Entry(offer).State = EntityState.Detached;
            return OfferResult.Fail(tr.Error ?? "The offer could not be issued.");
        }

        await _audit.LogAsync("OfferIssued", nameof(LoanOffer), offer.Id.ToString(), null,
            new { version, t.Principal, t.Rate, t.Fee, t.TotalRepayable, offer.ExpiresAt }, ct);
        return OfferResult.Ok("Offer issued to the borrower.");
    }

    // ---------- Accept ----------

    public async Task<OfferResult> AcceptAsync(Guid applicationId, string userId, CancellationToken ct = default)
    {
        var offer = await LiveOffer(applicationId, OfferStatus.Issued, ct);
        if (offer is null) return OfferResult.Fail("There is no open offer to accept.");
        if (offer.ExpiresAt <= DateTime.UtcNow) return OfferResult.Fail("This offer has expired. Ask us to issue a new one.");

        var app = await _db.LoanApplications.Include(a => a.Borrower)
            .FirstAsync(a => a.Id == applicationId, ct);

        var schedule = JsonSerializer.Deserialize<List<ScheduleLine>>(offer.ScheduleJson) ?? new();
        var number = $"AGR-{app.ApplicationNumber}-V{offer.Version}";
        var text = AgreementTemplate.Render(offer, schedule,
            $"{app.Borrower.FirstName} {app.Borrower.LastName}".Trim(), app.Borrower.NationalIdNumber, number);

        offer.Status = OfferStatus.Accepted;
        offer.AcceptedAt = DateTime.UtcNow;
        offer.AcceptedByUserId = userId;

        var agreement = new LoanAgreement
        {
            LoanApplicationId = applicationId,
            LoanOfferId = offer.Id,
            AgreementNumber = number,
            TemplateVersion = AgreementTemplate.Version,
            Text = text,
            Sha256Hash = Hash(text)
        };
        _db.LoanAgreements.Add(agreement);

        var tr = await _workflow.TransitionAsync(applicationId, LoanApplicationStatus.AgreementPending, userId,
            "Borrower accepted the offer.", ct);
        if (!tr.Succeeded)
        {
            _db.Entry(agreement).State = EntityState.Detached;
            await _db.Entry(offer).ReloadAsync(ct);
            return OfferResult.Fail(tr.Error ?? "The offer could not be accepted.");
        }

        await _audit.LogAsync("OfferAccepted", nameof(LoanOffer), offer.Id.ToString(), null,
            new { agreement.AgreementNumber, agreement.Sha256Hash }, ct);
        return OfferResult.Ok("Offer accepted. Please read and sign the agreement.");
    }

    // ---------- Decline ----------

    public async Task<OfferResult> DeclineAsync(Guid applicationId, string? reason, string userId,
        CancellationToken ct = default)
    {
        var offer = await LiveOffer(applicationId, OfferStatus.Issued, ct);
        if (offer is null) return OfferResult.Fail("There is no open offer to decline.");

        var why = string.IsNullOrWhiteSpace(reason) ? "Borrower declined the offer." : reason.Trim();
        offer.Status = OfferStatus.Declined;
        offer.DeclineReason = why;

        var tr = await _workflow.TransitionAsync(applicationId, LoanApplicationStatus.Cancelled, userId, why, ct);
        if (!tr.Succeeded)
        {
            await _db.Entry(offer).ReloadAsync(ct);
            return OfferResult.Fail(tr.Error ?? "The offer could not be declined.");
        }

        await _audit.LogAsync("OfferDeclined", nameof(LoanOffer), offer.Id.ToString(), why, null, ct);
        return OfferResult.Ok("You declined the offer. Your application is closed.");
    }

    // ---------- Sign ----------

    public async Task<OfferResult> SignAsync(Guid applicationId, string password, string userId, string? ip,
        string? userAgent, CancellationToken ct = default)
    {
        // Until counsel approves the template and signing method, signing is development-only.
        if (!_env.IsDevelopment() && !_config.GetValue<bool>("Agreements:TemplateApproved"))
            return OfferResult.Fail("Signing is not yet enabled. The agreement template is awaiting legal approval.");

        var offer = await LiveOffer(applicationId, OfferStatus.Accepted, ct);
        if (offer is null) return OfferResult.Fail("There is no accepted offer to sign.");
        if (offer.ExpiresAt <= DateTime.UtcNow) return OfferResult.Fail("This offer has expired.");

        var agreement = await _db.LoanAgreements
            .FirstOrDefaultAsync(a => a.LoanOfferId == offer.Id && a.Status == AgreementStatus.Generated, ct);
        if (agreement is null) return OfferResult.Fail("There is no agreement waiting for a signature.");
        if (Hash(agreement.Text) != agreement.Sha256Hash)
            return OfferResult.Fail("The agreement failed its integrity check. Contact us.");

        var user = await _users.FindByIdAsync(userId);
        if (user is null || !await _users.HasPasswordAsync(user))
            return OfferResult.Fail("Your account has no password, so it cannot sign this way. Set a password first.");

        if (!await _users.CheckPasswordAsync(user, password))
        {
            await _users.AccessFailedAsync(user);   // repeated failures lock the account
            await _audit.LogAsync("AgreementSignFailed", nameof(LoanAgreement), agreement.Id.ToString(),
                "Incorrect password at signing", null, ct);
            return OfferResult.Fail("Incorrect password.");
        }
        await _users.ResetAccessFailedCountAsync(user);

        var signature = new AgreementSignature
        {
            LoanAgreementId = agreement.Id,
            SignedByUserId = userId,
            SignedAt = DateTime.UtcNow,
            Method = "PasswordReauthentication",
            AgreementHash = agreement.Sha256Hash,
            IpAddress = ip,
            UserAgent = userAgent is { Length: > 500 } ? userAgent[..500] : userAgent
        };
        _db.AgreementSignatures.Add(signature);
        agreement.Status = AgreementStatus.Signed;

        var tr = await _workflow.TransitionAsync(applicationId, LoanApplicationStatus.ReadyForDisbursement, userId,
            "Borrower signed the agreement.", ct);
        if (!tr.Succeeded)
        {
            _db.Entry(signature).State = EntityState.Detached;
            await _db.Entry(agreement).ReloadAsync(ct);
            return OfferResult.Fail(tr.Error ?? "The agreement could not be signed.");
        }

        await _audit.LogAsync("AgreementSigned", nameof(LoanAgreement), agreement.Id.ToString(), null,
            new { agreement.AgreementNumber, signature.Method, agreement.Sha256Hash }, ct);
        return OfferResult.Ok("Agreement signed. Your loan is now being prepared for disbursement.");
    }

    // ---------- Expiry (system action) ----------

    public async Task<int> ExpireOverdueAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var overdue = await _db.LoanOffers
            .Include(o => o.LoanApplication)
            .Where(o => (o.Status == OfferStatus.Issued || o.Status == OfferStatus.Accepted) && o.ExpiresAt < now)
            .ToListAsync(ct);

        var count = 0;
        foreach (var o in overdue)
        {
            var app = o.LoanApplication;
            if (app.Status is not (LoanApplicationStatus.OfferIssued or LoanApplicationStatus.AgreementPending)) continue;
            if (!_workflow.IsTransitionAllowed(app.Status, LoanApplicationStatus.Approved)) continue;

            var from = app.Status;
            o.Status = OfferStatus.Expired;

            var agreements = await _db.LoanAgreements
                .Where(a => a.LoanOfferId == o.Id && a.Status == AgreementStatus.Generated).ToListAsync(ct);
            foreach (var a in agreements) a.Status = AgreementStatus.Expired;

            app.Status = LoanApplicationStatus.Approved;   // back to Approved so a new offer can be issued
            _db.LoanApplicationStatusHistories.Add(new LoanApplicationStatusHistory
            {
                LoanApplicationId = app.Id,
                PreviousStatus = from,
                NewStatus = LoanApplicationStatus.Approved,
                ChangedAt = now,
                ChangedByUserId = null,
                Notes = $"Offer version {o.Version} expired."
            });
            _audit.Record("OfferExpired", nameof(LoanOffer), o.Id.ToString(), null, new { o.Version });
            count++;
        }

        if (count > 0) await _db.SaveChangesAsync(ct);
        return count;
    }

    // ---------- helpers ----------

    private Task<LoanOffer?> LiveOffer(Guid applicationId, OfferStatus status, CancellationToken ct)
        => _db.LoanOffers.Where(o => o.LoanApplicationId == applicationId && o.Status == status)
            .OrderByDescending(o => o.Version).FirstOrDefaultAsync(ct);

    public static string Hash(string text) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
}