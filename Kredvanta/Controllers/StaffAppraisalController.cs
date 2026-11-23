using System.Security.Claims;
using Kredvanta.Authorization;
using Kredvanta.Data;
using Kredvanta.Services.Audit;
using Kredvanta.Services.Credit;
using Kredvanta.Services.Workflow;
using Kredvanta.ViewModels.Staff;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static Kredvanta.Models.Enums.LendingEnums;

namespace Kredvanta.Controllers;

[Authorize(Policy = "CaseReader")]
[Route("staff/appraisal")]
public class StaffAppraisalController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IAppraisalService _appraisal;
    private readonly IApplicationWorkflowService _workflow;
    private readonly IAuditService _audit;

    public StaffAppraisalController(ApplicationDbContext db, IAppraisalService appraisal,
        IApplicationWorkflowService workflow, IAuditService audit)
    {
        _db = db; _appraisal = appraisal; _workflow = workflow; _audit = audit;
    }

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet("{applicationId:guid}")]
    public async Task<IActionResult> Index(Guid applicationId, decimal existing = 0, decimal other = 0, decimal expenses = 0)
    {
        var app = await _db.LoanApplications.AsNoTracking()
            .Include(a => a.Borrower).Include(a => a.LoanProduct)
            .FirstOrDefaultAsync(a => a.Id == applicationId);
        if (app is null) return NotFound();

        await _audit.LogAsync("AppraisalViewed", "LoanApplication", applicationId.ToString());

        var input = new AppraisalInput
        {
            ExistingMonthlyLoanPayments = existing,
            OtherMonthlyObligations = other,
            MonthlyExpenses = expenses
        };

        var current = await _db.CreditAssessments.AsNoTracking()
            .Where(a => a.LoanApplicationId == applicationId && a.IsCurrent)
            .Select(a => new CurrentAssessmentView
            {
                Recommendation = a.Recommendation,
                Ratio = a.DebtServiceRatio,
                Income = a.MonthlyIncome,
                Instalment = a.ProposedMonthlyInstallment,
                DisposableBefore = a.DisposableIncome,
                Flags = a.RiskFlags,
                Notes = a.Notes,
                AssessedAt = a.AssessedAt,
                Stale = a.AssessedAmount != app.RequestedAmount || a.AssessedTermPeriods != app.RequestedTermPeriods
            })
            .FirstOrDefaultAsync();

        var isAnalyst = User.IsInRole(AppRoles.CreditAnalyst);

        return View(new AppraisalPage
        {
            ApplicationId = app.Id,
            ApplicationNumber = app.ApplicationNumber,
            Status = app.Status,
            BorrowerName = $"{app.Borrower.FirstName} {app.Borrower.LastName}".Trim(),
            ProductName = app.LoanProduct.ProductName,
            Amount = app.RequestedAmount,
            Term = app.RequestedTermPeriods,
            Preview = await _appraisal.PreviewAsync(applicationId, input),
            Input = input,
            Current = current,
            CanAssess = isAnalyst && app.Status == LoanApplicationStatus.UnderReview
                        && app.Borrower.ApplicationUserId != UserId,
            CanRecommend = isAnalyst && app.Status == LoanApplicationStatus.UnderReview
                           && current is { Stale: false }
                           && current.Recommendation is CreditRecommendation.Recommend
                               or CreditRecommendation.RecommendWithConditions
        });
    }

    [HttpPost("{applicationId:guid}/assess")]
    [Authorize(Roles = AppRoles.CreditAnalyst)]
    public async Task<IActionResult> Assess(Guid applicationId, AppraisalInput input)
    {
        var r = await _appraisal.AssessAsync(applicationId, input, UserId);
        TempData[r.Succeeded ? "Success" : "Error"] =
            r.Succeeded ? $"Assessment recorded: {r.Recommendation}." : r.Error;
        return RedirectToAction(nameof(Index), new { applicationId });
    }

    [HttpPost("{applicationId:guid}/recommend")]
    [Authorize(Roles = AppRoles.CreditAnalyst)]
    public async Task<IActionResult> Recommend(Guid applicationId, string? notes)
    {
        var r = await _workflow.TransitionAsync(applicationId, LoanApplicationStatus.Recommended, UserId,
            string.IsNullOrWhiteSpace(notes) ? "Recommended after credit assessment." : notes);
        TempData[r.Succeeded ? "Success" : "Error"] =
            r.Succeeded ? "Application recommended for approval." : r.Error;
        return RedirectToAction(nameof(Index), new { applicationId });
    }
}