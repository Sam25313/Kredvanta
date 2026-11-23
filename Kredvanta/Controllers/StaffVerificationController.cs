using System.Security.Claims;
using Kredvanta.Authorization;
using Kredvanta.Data;
using Kredvanta.Services.Audit;
using Kredvanta.Services.Verification;
using Kredvanta.ViewModels.Staff;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static Kredvanta.Models.Enums.LendingEnums;

namespace Kredvanta.Controllers;

[Authorize(Policy = "CaseReader")]
[Route("staff/verification")]
public class StaffVerificationController : Controller
{
    private static readonly LoanApplicationStatus[] Open =
    {
        LoanApplicationStatus.Submitted, LoanApplicationStatus.UnderReview,
        LoanApplicationStatus.DocumentsRequired
    };

    private readonly ApplicationDbContext _db;
    private readonly IVerificationService _verification;
    private readonly IAuditService _audit;

    public StaffVerificationController(ApplicationDbContext db, IVerificationService verification, IAuditService audit)
    {
        _db = db; _verification = verification; _audit = audit;
    }

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var items = await _db.LoanApplications.AsNoTracking()
            .Where(a => Open.Contains(a.Status))
            .OrderBy(a => a.SubmittedAt)
            .Take(200)
            .Select(a => new VerificationQueueItem
            {
                ApplicationId = a.Id,
                ApplicationNumber = a.ApplicationNumber,
                BorrowerName = a.Borrower.FirstName + " " + a.Borrower.LastName,
                Status = a.Status,
                SubmittedAt = a.SubmittedAt,
                KycStatus = a.Borrower.KycStatus,
                EmploymentState = _db.EmploymentVerifications
                    .Where(v => v.LoanApplicationId == a.Id)
                    .OrderByDescending(v => v.AttemptedAt)
                    .Select(v => v.Outcome.ToString())
                    .FirstOrDefault() ?? "Not started"
            })
            .ToListAsync();

        return View(items);
    }

    [HttpGet("{applicationId:guid}")]
    public async Task<IActionResult> Case(Guid applicationId)
    {
        var app = await _db.LoanApplications.AsNoTracking()
            .Include(a => a.Borrower)
            .FirstOrDefaultAsync(a => a.Id == applicationId);
        if (app is null) return NotFound();

        await _audit.LogAsync("VerificationCaseViewed", "LoanApplication", applicationId.ToString());

        var b = app.Borrower;
        var open = Open.Contains(app.Status);

        var kyc = await _db.KycReviews.AsNoTracking()
            .Where(r => r.BorrowerId == b.Id).OrderByDescending(r => r.ReviewedAt)
            .Select(r => new { r.ReviewedAt, r.Outcome, r.Notes, By = r.ReviewedByUser!.FirstName + " " + r.ReviewedByUser.LastName })
            .ToListAsync();

        var attempts = await _db.EmploymentVerifications.AsNoTracking()
            .Where(v => v.LoanApplicationId == applicationId).OrderByDescending(v => v.AttemptedAt)
            .Select(v => new
            {
                v.AttemptedAt,
                v.Outcome,
                v.Channel,
                v.ContactedParty,
                v.ContactIndependentlySourced,
                v.Notes,
                By = v.VerifiedByUser!.FirstName + " " + v.VerifiedByUser.LastName
            })
            .ToListAsync();

        var model = new VerificationCase
        {
            ApplicationId = app.Id,
            ApplicationNumber = app.ApplicationNumber,
            Status = app.Status,
            BorrowerName = $"{b.FirstName} {b.LastName}".Trim(),
            NationalId = b.NationalIdNumber,
            DateOfBirth = b.DateOfBirth,
            KycStatus = b.KycStatus,
            Employer = b.EmployerOrBusinessName ?? "",
            JobTitle = b.JobTitle ?? "",
            ContractType = b.EmploymentContractType?.ToString() ?? "",
            StartDate = b.EmploymentStartDate,
            NetSalary = b.NetMonthlySalary,
            CanDoKyc = open && (User.IsInRole(AppRoles.Administrator) || User.IsInRole(AppRoles.KycOfficer)) && b.ApplicationUserId != UserId,
            CanDoEmployment = open && (User.IsInRole(AppRoles.Administrator) || User.IsInRole(AppRoles.VerificationOfficer)) && b.ApplicationUserId != UserId,
            Kyc = kyc.Select(r => (r.ReviewedAt, r.Outcome.ToString(), r.By, r.Notes)).ToList(),
            Attempts = attempts.Select(v => (v.AttemptedAt, v.Outcome.ToString(), v.Channel.ToString(),
                v.ContactedParty, v.ContactIndependentlySourced, v.By, v.Notes)).ToList()
        };

        return View(model);
    }

    [HttpPost("{applicationId:guid}/kyc")]
    [Authorize(Roles = $"{AppRoles.Administrator},{AppRoles.KycOfficer}")]
    public async Task<IActionResult> Kyc(Guid applicationId, KycStatus outcome,
        bool identityMatchesProfile, bool documentsAppearGenuine, string? reasonCode, string? notes)
    {
        var r = await _verification.RecordKycAsync(applicationId, outcome, identityMatchesProfile,
            documentsAppearGenuine, reasonCode, notes, UserId);
        TempData[r.Succeeded ? "Success" : "Error"] = r.Succeeded ? "KYC review recorded." : r.Error;
        return RedirectToAction(nameof(Case), new { applicationId });
    }

    [HttpPost("{applicationId:guid}/employment")]
    [Authorize(Roles = $"{AppRoles.Administrator},{AppRoles.VerificationOfficer}")]
    public async Task<IActionResult> Employment(Guid applicationId, EmploymentVerificationInput input)
    {
        var r = await _verification.RecordEmploymentAsync(applicationId, input, UserId);
        TempData[r.Succeeded ? "Success" : "Error"] = r.Succeeded ? "Verification attempt recorded." : r.Error;
        return RedirectToAction(nameof(Case), new { applicationId });
    }
}