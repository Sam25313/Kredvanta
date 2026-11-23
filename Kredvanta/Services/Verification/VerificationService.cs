using Kredvanta.Data;
using Kredvanta.Models.Verification;
using Kredvanta.Services.Audit;
using Microsoft.EntityFrameworkCore;
using static Kredvanta.Models.Enums.LendingEnums;

namespace Kredvanta.Services.Verification;

public class VerificationService : IVerificationService
{
    private static readonly LoanApplicationStatus[] OpenStatuses =
    {
        LoanApplicationStatus.Submitted, LoanApplicationStatus.UnderReview,
        LoanApplicationStatus.DocumentsRequired
    };

    private readonly ApplicationDbContext _db;
    private readonly IAuditService _audit;

    public VerificationService(ApplicationDbContext db, IAuditService audit)
    {
        _db = db; _audit = audit;
    }

    public async Task<VerificationResult> RecordKycAsync(Guid applicationId, KycStatus outcome,
        bool identityMatchesProfile, bool documentsAppearGenuine,
        string? reasonCode, string? notes, string reviewerId, CancellationToken ct = default)
    {
        var app = await _db.LoanApplications.Include(a => a.Borrower)
            .FirstOrDefaultAsync(a => a.Id == applicationId, ct);
        if (app is null) return VerificationResult.Fail("Application not found.");
        if (!OpenStatuses.Contains(app.Status))
            return VerificationResult.Fail("This application is not open for verification.");

        var borrower = app.Borrower;

        if (borrower.ApplicationUserId == reviewerId)
            return VerificationResult.Fail("You cannot verify your own identity.");

        if (outcome is KycStatus.NotStarted)
            return VerificationResult.Fail("Choose an outcome.");

        if (outcome == KycStatus.Verified && !(identityMatchesProfile && documentsAppearGenuine))
            return VerificationResult.Fail(
                "Confirm that the identity matches the profile and the documents appear genuine before verifying.");

        if (outcome is KycStatus.Rejected or KycStatus.Escalated && string.IsNullOrWhiteSpace(notes))
            return VerificationResult.Fail("Notes are required when rejecting or escalating.");

        var previous = borrower.KycStatus;
        borrower.KycStatus = outcome;

        _db.KycReviews.Add(new KycReview
        {
            BorrowerId = borrower.Id,
            LoanApplicationId = applicationId,
            PreviousStatus = previous,
            Outcome = outcome,
            IdentityMatchesProfile = identityMatchesProfile,
            DocumentsAppearGenuine = documentsAppearGenuine,
            ReasonCode = reasonCode,
            Notes = notes?.Trim(),
            ReviewedByUserId = reviewerId
        });

        _audit.Record("KycReviewed", "Borrower", borrower.Id.ToString(), null,
            new { ApplicationId = applicationId, From = previous.ToString(), To = outcome.ToString() });

        await _db.SaveChangesAsync(ct);
        return VerificationResult.Ok();
    }

    public async Task<VerificationResult> RecordEmploymentAsync(Guid applicationId,
        EmploymentVerificationInput i, string verifierId, CancellationToken ct = default)
    {
        var app = await _db.LoanApplications.Include(a => a.Borrower)
            .FirstOrDefaultAsync(a => a.Id == applicationId, ct);
        if (app is null) return VerificationResult.Fail("Application not found.");
        if (!OpenStatuses.Contains(app.Status))
            return VerificationResult.Fail("This application is not open for verification.");

        var b = app.Borrower;
        if (b.ApplicationUserId == verifierId)
            return VerificationResult.Fail("You cannot verify your own employment.");

        if (string.IsNullOrWhiteSpace(i.ContactedParty) || string.IsNullOrWhiteSpace(i.ContactDetailUsed))
            return VerificationResult.Fail("Record who you contacted and the contact detail you used.");

        if (i.Outcome == EmploymentVerificationOutcome.Verified)
        {
            if (!i.ContactIndependentlySourced)
                return VerificationResult.Fail(
                    "Employment cannot be verified using contact details supplied by the applicant. " +
                    "Source the employer's contact independently, or record the attempt as unverified.");

            if (string.IsNullOrWhiteSpace(i.ContactPersonName) ||
                i.ConfirmedContractType is null || i.ConfirmedStartDate is null ||
                i.ConfirmedNetSalary is null or <= 0)
                return VerificationResult.Fail(
                    "Record the contact person and the confirmed contract type, start date and net salary.");

            var differs = i.ConfirmedContractType != b.EmploymentContractType ||
                          i.ConfirmedStartDate != b.EmploymentStartDate ||
                          i.ConfirmedNetSalary != b.NetMonthlySalary;
            if (differs && string.IsNullOrWhiteSpace(i.Notes))
                return VerificationResult.Fail(
                    "The confirmed details differ from what the applicant declared. Explain the difference in the notes.");
        }
        else if (i.Outcome is EmploymentVerificationOutcome.NotVerified or EmploymentVerificationOutcome.Escalated
                 && string.IsNullOrWhiteSpace(i.Notes))
        {
            return VerificationResult.Fail("Notes are required for this outcome.");
        }

        _db.EmploymentVerifications.Add(new EmploymentVerification
        {
            LoanApplicationId = applicationId,
            Channel = i.Channel,
            ContactedParty = i.ContactedParty.Trim(),
            ContactPersonName = i.ContactPersonName?.Trim(),
            ContactDetailUsed = i.ContactDetailUsed.Trim(),
            ContactIndependentlySourced = i.ContactIndependentlySourced,
            Outcome = i.Outcome,
            ConfirmedJobTitle = i.ConfirmedJobTitle?.Trim(),
            ConfirmedContractType = i.ConfirmedContractType,
            ConfirmedStartDate = i.ConfirmedStartDate,
            ConfirmedNetSalary = i.ConfirmedNetSalary,
            Notes = i.Notes?.Trim(),
            VerifiedByUserId = verifierId
        });

        _audit.Record("EmploymentVerificationRecorded", "LoanApplication", applicationId.ToString(), null,
            new { i.Channel, Outcome = i.Outcome.ToString(), i.ContactIndependentlySourced });

        await _db.SaveChangesAsync(ct);
        return VerificationResult.Ok();
    }
}