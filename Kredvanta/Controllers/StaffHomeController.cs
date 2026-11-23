using Kredvanta.Authorization;
using Kredvanta.Data;
using Kredvanta.ViewModels.Staff;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static Kredvanta.Models.Enums.LendingEnums;

namespace Kredvanta.Controllers;

[Authorize(Policy = "CaseReader")]
[Route("staff")]
public class StaffHomeController : Controller
{
    private readonly ApplicationDbContext _db;
    public StaffHomeController(ApplicationDbContext db) => _db = db;

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        bool Has(params string[] roles) => roles.Any(User.IsInRole);

        var apps = _db.LoanApplications.AsNoTracking();
        var open = new[]
        {
            LoanApplicationStatus.Submitted,
            LoanApplicationStatus.UnderReview,
            LoanApplicationStatus.DocumentsRequired
        };

        // Same definition the guards use: a verified check with an independently sourced contact.
        var cards = new List<WorkCard>();

        // Stage 1: intake
        if (Has(AppRoles.LoanOfficer))
        {
            cards.Add(new WorkCard(1, "New applications", "Submitted and waiting for someone to start the review.",
                await apps.CountAsync(a => a.Status == LoanApplicationStatus.Submitted),
                Url.Action("Index", "StaffApplications", new { status = LoanApplicationStatus.Submitted })!));

            cards.Add(new WorkCard(2, "Waiting for the applicant", "You asked for more documents. Nothing to do until they respond.",
                await apps.CountAsync(a => a.Status == LoanApplicationStatus.DocumentsRequired),
                Url.Action("Index", "StaffApplications", new { status = LoanApplicationStatus.DocumentsRequired })!));
        }

        // Stage 3: documents
        if (Has(AppRoles.VerificationOfficer, AppRoles.KycOfficer))
        {
            cards.Add(new WorkCard(3, "Documents to review", "Uploaded documents that nobody has accepted or rejected yet.",
                await _db.LoanApplicationDocuments.CountAsync(d =>
                    d.IsCurrent && d.ReviewStatus == DocumentReviewStatus.Pending &&
                    (d.LoanApplication.Status == LoanApplicationStatus.UnderReview ||
                     d.LoanApplication.Status == LoanApplicationStatus.DocumentsRequired)),
                Url.Action("Index", "StaffApplications", new { status = LoanApplicationStatus.UnderReview })!));
        }

        // Stage 4: identity
        if (Has(AppRoles.KycOfficer))
        {
            cards.Add(new WorkCard(4, "Identity checks", "Open applications where the applicant's identity is not verified yet.",
                await apps.CountAsync(a => open.Contains(a.Status) && a.Borrower.KycStatus != KycStatus.Verified),
                Url.Action("Index", "StaffVerification")!));
        }

        // Stage 5: employer
        if (Has(AppRoles.VerificationOfficer))
        {
            cards.Add(new WorkCard(5, "Employer checks", "Open applications without an independently verified employer confirmation.",
                await apps.CountAsync(a => open.Contains(a.Status) &&
                    !_db.EmploymentVerifications.Any(v => v.LoanApplicationId == a.Id &&
                        v.Outcome == EmploymentVerificationOutcome.Verified && v.ContactIndependentlySourced)),
                Url.Action("Index", "StaffVerification")!));
        }

        // Stage 6: assessment
        if (Has(AppRoles.CreditAnalyst))
        {
            cards.Add(new WorkCard(6, "Ready to assess", "Under review with employment verified and no up-to-date assessment.",
                await apps.CountAsync(a => a.Status == LoanApplicationStatus.UnderReview &&
                    _db.EmploymentVerifications.Any(v => v.LoanApplicationId == a.Id &&
                        v.Outcome == EmploymentVerificationOutcome.Verified && v.ContactIndependentlySourced) &&
                    !a.CreditAssessments.Any(c => c.IsCurrent &&
                        c.AssessedAmount == a.RequestedAmount &&
                        c.AssessedTermPeriods == a.RequestedTermPeriods)),
                Url.Action("Index", "StaffApplications", new { status = LoanApplicationStatus.UnderReview })!));
        }

        // Stage 7: decision
        if (Has(AppRoles.CreditManager))
        {
            cards.Add(new WorkCard(7, "Awaiting your decision", "Recommended by an analyst and waiting for a credit manager.",
                await apps.CountAsync(a => a.Status == LoanApplicationStatus.Recommended),
                Url.Action("Index", "StaffDecisions")!));
        }

        // Stage 8: offers
        if (Has(AppRoles.LoanOfficer))
        {
            cards.Add(new WorkCard(8, "Offers to prepare", "Approved applications with a recorded manager approval, ready for offer preparation.",
                await apps.CountAsync(a => a.Status == LoanApplicationStatus.Approved &&
                    _db.LoanApprovals.Any(approval => approval.LoanApplicationId == a.Id &&
                        approval.Decision == ApprovalDecision.Approved)),
                Url.Action("Index", "StaffOffers")!));
        }

        // Auditor
        if (Has(AppRoles.Auditor))
        {
            var since = DateTime.UtcNow.AddHours(-24);
            cards.Add(new WorkCard(9, "Audit events in the last 24 hours", "Open the audit log to filter and review them.",
                await _db.AuditLogs.CountAsync(a => a.ActionDate >= since),
                Url.Action("Index", "AuditLog")!, ShowAllClear: false));
        }

        return View(new StaffHomeViewModel
        {
            Cards = cards.OrderBy(c => c.Stage).ToList()
        });
    }
}