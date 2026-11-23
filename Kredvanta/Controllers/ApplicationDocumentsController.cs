using System.Security.Claims;
using Kredvanta.Authorization;
using Kredvanta.Data;
using Kredvanta.Services.Audit;
using Kredvanta.Services.Documents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static Kredvanta.Models.Enums.LendingEnums;

namespace Kredvanta.Controllers;

[Authorize]
[Route("applications/{applicationId:guid}/documents")]
public class ApplicationDocumentsController : Controller
{
    private const long MaxRequestBytes = 6 * 1024 * 1024;   // just above the 5 MB file limit

    private readonly ApplicationDbContext _db;
    private readonly IDocumentService _documents;
    private readonly IAuthorizationService _authorization;
    private readonly IAuditService _audit;

    public ApplicationDocumentsController(ApplicationDbContext db, IDocumentService documents,
        IAuthorizationService authorization, IAuditService audit)
    {
        _db = db; _documents = documents; _authorization = authorization; _audit = audit;
    }

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    private IActionResult Back(Guid applicationId, string? returnTo)
        => returnTo == "dashboard"
            ? RedirectToAction("Index", "Dashboard")
            : RedirectToAction("Details", "LoanApplications", new { id = applicationId });

    // ---------- Borrower: upload or replace ----------

    [HttpPost("upload")]
    [Authorize(Roles = AppRoles.Borrower)]
    [RequestSizeLimit(MaxRequestBytes)]
    public async Task<IActionResult> Upload(Guid applicationId, Guid requiredDocumentId,
        IFormFile? file, string? returnTo)
    {
        var app = await _db.LoanApplications.FindAsync(applicationId);
        if (app is null) return NotFound();

        if (!(await _authorization.AuthorizeAsync(User, app, Operations.Modify)).Succeeded)
        {
            await _audit.LogAsync("AccessDenied", "LoanApplication", applicationId.ToString(),
                "Attempted to upload to an application they do not own");
            return Forbid();
        }

        if (file is null || file.Length == 0)
        {
            TempData["Error"] = "Choose a file to upload.";
            return Back(applicationId, returnTo);
        }

        await using var stream = file.OpenReadStream();
        var result = await _documents.UploadAsync(applicationId, requiredDocumentId,
            stream, file.FileName, UserId);

        TempData[result.Succeeded ? "Success" : "Error"] =
            result.Succeeded ? "Document uploaded. We're checking it now." : result.Error;

        return Back(applicationId, returnTo);
    }

    // ---------- Borrower: live screening status (polled by the Details page) ----------

    [HttpGet("screening-status")]
    [Authorize(Roles = AppRoles.Borrower)]
    public async Task<IActionResult> ScreeningStatus(Guid applicationId)
    {
        var app = await _db.LoanApplications.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == applicationId);
        if (app is null) return NotFound();

        if (!(await _authorization.AuthorizeAsync(User, app, Operations.Read)).Succeeded)
            return Forbid();

        var rows = await _db.LoanApplicationDocuments.AsNoTracking()
            .Where(d => d.LoanApplicationId == applicationId && d.IsCurrent)
            .OrderBy(d => d.CreatedAt)
            .Select(d => new { d.Id, d.ScreeningStatus })
            .ToListAsync();

        var documents = rows.Select(r => new
        {
            id = r.Id,
            status = r.ScreeningStatus.ToString(),
            message = ApplicantMessage(r.ScreeningStatus)
        });

        var done = rows.All(r => r.ScreeningStatus is not
            (DocumentScreeningStatus.Queued or DocumentScreeningStatus.Scanning));

        return Json(new { done, documents });
    }

    // Applicants only ever see generic wording. Detailed check results are for staff.
    private static string ApplicantMessage(DocumentScreeningStatus status) => status switch
    {
        DocumentScreeningStatus.Queued => "Waiting to be checked",
        DocumentScreeningStatus.Scanning => "Checking against your profile...",
        DocumentScreeningStatus.Passed => "Automatic checks passed",
        DocumentScreeningStatus.Flagged =>
            "We couldn't confirm everything automatically. You can replace this document, or continue and a loan officer will review it.",
        DocumentScreeningStatus.Failed =>
            "We couldn't check this file automatically. A loan officer will review it.",
        _ => "Not checked"
    };

    // ---------- Download (borrower owner or staff case readers) ----------

    [HttpGet("{documentId:guid}/download")]
    public async Task<IActionResult> Download(Guid applicationId, Guid documentId)
    {
        var app = await _db.LoanApplications.FindAsync(applicationId);
        if (app is null) return NotFound();

        if (!(await _authorization.AuthorizeAsync(User, app, Operations.Read)).Succeeded)
        {
            await _audit.LogAsync("AccessDenied", "LoanApplicationDocument", documentId.ToString(),
                "Attempted to download a document without access");
            return Forbid();
        }

        var opened = await _documents.OpenAsync(documentId);
        // The document must belong to the application that was authorized.
        if (opened is null || opened.Value.Document.LoanApplicationId != applicationId)
            return NotFound();

        await _audit.LogAsync("DocumentDownloaded", "LoanApplicationDocument", documentId.ToString());

        var doc = opened.Value.Document;
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        return File(opened.Value.Content, doc.ContentType, doc.OriginalFileName);
    }

    // ---------- Staff: review ----------

    [HttpPost("{documentId:guid}/review")]
    [Authorize(Roles = $"{AppRoles.Administrator},{AppRoles.VerificationOfficer},{AppRoles.KycOfficer}")]
    public async Task<IActionResult> Review(Guid applicationId, Guid documentId,
        DocumentReviewStatus outcome, string? reasonCode, string? comments)
    {
        var belongs = await _db.LoanApplicationDocuments
            .AnyAsync(d => d.Id == documentId && d.LoanApplicationId == applicationId);
        if (!belongs) return NotFound();

        var result = await _documents.ReviewAsync(documentId, outcome, reasonCode, comments, UserId);

        TempData[result.Succeeded ? "Success" : "Error"] =
            result.Succeeded ? "Review recorded." : result.Error;

        return RedirectToAction("Details", "StaffApplications", new { id = applicationId });
    }
}