using System.Security.Claims;
using Kredvanta.Authorization;
using Kredvanta.Data;
using Kredvanta.Services.Audit;
using Kredvanta.Services.Workflow;
using Kredvanta.ViewModels.Staff;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static Kredvanta.Models.Enums.LendingEnums;

namespace Kredvanta.Controllers;

[Authorize(Policy = "CaseReader")]
[Route("staff/applications")]
public class StaffApplicationsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IApplicationWorkflowService _workflow;
    private readonly IAuditService _audit;

    public StaffApplicationsController(ApplicationDbContext db,
        IApplicationWorkflowService workflow, IAuditService audit)
    {
        _db = db; _workflow = workflow; _audit = audit;
    }

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    // ---------- Queue ----------

    [HttpGet("")]
    public async Task<IActionResult> Index(LoanApplicationStatus? status)
    {
        var query = _db.LoanApplications.AsNoTracking()
            .Where(a => a.Status != LoanApplicationStatus.Draft);

        if (status.HasValue) query = query.Where(a => a.Status == status.Value);

        var items = await query
            .OrderBy(a => a.SubmittedAt)
            .Take(200)
            .Select(a => new StaffApplicationListItem
            {
                Id = a.Id,
                ApplicationNumber = a.ApplicationNumber,
                BorrowerName = a.Borrower.FirstName + " " + a.Borrower.LastName,
                ProductName = a.LoanProduct.ProductName,
                RequestedAmount = a.RequestedAmount,
                Status = a.Status,
                SubmittedAt = a.SubmittedAt,
                PendingDocuments = a.Documents.Count(d => d.IsCurrent &&
                    d.ReviewStatus == DocumentReviewStatus.Pending)
            })
            .ToListAsync();

        ViewBag.StatusFilter = status;
        return View(items);
    }

    // ---------- Details / review panel ----------

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Details(Guid id)
    {
        var app = await _db.LoanApplications.AsNoTracking()
            .Include(a => a.Borrower)
            .Include(a => a.LoanProduct)
            .Include(a => a.Documents).ThenInclude(d => d.RequiredDocument)
            .Include(a => a.StatusHistory).ThenInclude(h => h.ChangedByUser)
            .FirstOrDefaultAsync(a => a.Id == id);
        if (app is null) return NotFound();

        await _audit.LogAsync("ApplicationViewedByStaff", "LoanApplication", id.ToString());

        var b = app.Borrower;
        var isAdministrator = User.IsInRole(AppRoles.Administrator);
        var isDocReviewer = isAdministrator || User.IsInRole(AppRoles.VerificationOfficer) || User.IsInRole(AppRoles.KycOfficer);
        var isOperator = isAdministrator || isDocReviewer || User.IsInRole(AppRoles.LoanOfficer)
                         || User.IsInRole(AppRoles.CreditAnalyst) || User.IsInRole(AppRoles.CreditManager);

        var model = new StaffApplicationDetails
        {
            Id = app.Id,
            ApplicationNumber = app.ApplicationNumber,
            Status = app.Status,
            ProductName = app.LoanProduct.ProductName,
            RequestedAmount = app.RequestedAmount,
            RequestedTermPeriods = app.RequestedTermPeriods,
            LoanPurpose = app.LoanPurpose,
            SubmittedAt = app.SubmittedAt,

            BorrowerName = $"{b.FirstName} {b.LastName}".Trim(),
            BorrowerNumber = b.BorrowerNumber,
            KycStatus = b.KycStatus.ToString(),
            EmploymentStatus = b.EmploymentStatus?.ToString() ?? "Not provided",
            Employer = b.EmployerOrBusinessName ?? "Not provided",
            ContractType = b.EmploymentContractType?.ToString() ?? "Not provided",
            ContractEndDate = b.EmploymentEndDate,
            GrossSalary = b.GrossMonthlySalary,
            NetSalary = b.NetMonthlySalary,

            CanReviewDocuments = isDocReviewer &&
                app.Status is LoanApplicationStatus.UnderReview or LoanApplicationStatus.DocumentsRequired,
            CanStartReview = isOperator && app.Status == LoanApplicationStatus.Submitted,
            CanRequestDocuments = isOperator && app.Status == LoanApplicationStatus.UnderReview,
            CanResumeReview = isOperator && app.Status == LoanApplicationStatus.DocumentsRequired,

            Documents = app.Documents
                .OrderByDescending(d => d.CreatedAt)
                .Select(d => new StaffDocumentItem
                {
                    Id = d.Id,
                    RequirementName = d.RequiredDocument?.DocumentName ?? "Additional document",
                    FileName = d.OriginalFileName,
                    Version = d.Version,
                    IsCurrent = d.IsCurrent,
                    ReviewStatus = d.ReviewStatus,
                    RejectionReasonCode = d.RejectionReasonCode,
                    ReviewComments = d.ReviewComments,
                    UploadedAt = d.CreatedAt,
                    Sha256Hash = d.Sha256Hash,
                    UploadedByCurrentUser = d.UploadedByUserId == UserId
                }).ToList(),

            History = app.StatusHistory
                .OrderByDescending(h => h.ChangedAt)
                .Select(h => new StaffHistoryItem
                {
                    From = h.PreviousStatus,
                    To = h.NewStatus,
                    At = h.ChangedAt,
                    By = h.ChangedByUser == null ? "System" : $"{h.ChangedByUser.FirstName} {h.ChangedByUser.LastName}",
                    Notes = h.Notes
                }).ToList()
        };

        return View(model);
    }

    // ---------- Workflow actions ----------

    [HttpPost("{id:guid}/start-review")]
    public async Task<IActionResult> StartReview(Guid id)
    {
        var result = await _workflow.TransitionAsync(id, LoanApplicationStatus.UnderReview, UserId,
            "Review started.");

        if (result.Succeeded)
        {
            var app = await _db.LoanApplications.FirstAsync(a => a.Id == id);
            app.AssignedOfficerId ??= UserId;
            await _db.SaveChangesAsync();
            TempData["Success"] = "Review started.";
        }
        else TempData["Error"] = result.Error;

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost("{id:guid}/request-documents")]
    public async Task<IActionResult> RequestDocuments(Guid id, string? notes)
    {
        var result = await _workflow.TransitionAsync(id, LoanApplicationStatus.DocumentsRequired, UserId, notes);
        TempData[result.Succeeded ? "Success" : "Error"] =
            result.Succeeded ? "The applicant has been asked for more documents." : result.Error;
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost("{id:guid}/resume-review")]
    public async Task<IActionResult> ResumeReview(Guid id)
    {
        var result = await _workflow.TransitionAsync(id, LoanApplicationStatus.UnderReview, UserId,
            "Review resumed after applicant action.");
        TempData[result.Succeeded ? "Success" : "Error"] =
            result.Succeeded ? "Review resumed." : result.Error;
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost("{id:guid}/decline")]
    [Authorize(Roles = AppRoles.CreditManager)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Decline(Guid id, string? reason)
    {
        var result = await _workflow.TransitionAsync(id, LoanApplicationStatus.Rejected, UserId, reason);
        TempData[result.Succeeded ? "Success" : "Error"] =
            result.Succeeded ? "The application was declined." : result.Error;
        return RedirectToAction(nameof(Details), new { id });
    }
}