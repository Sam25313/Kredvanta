using System.Security.Claims;
using Kredvanta.Authorization;
using Kredvanta.Data;
using Kredvanta.Services.Audit;
using Kredvanta.Services.Credit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static Kredvanta.Models.Enums.LendingEnums;

namespace Kredvanta.Controllers;

[Authorize(Roles = AppRoles.CreditManager)]
[Route("staff/decisions")]
public class StaffDecisionsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IApprovalService _approvals;
    private readonly IAuditService _audit;

    public StaffDecisionsController(ApplicationDbContext db, IApprovalService approvals, IAuditService audit)
    { _db = db; _approvals = approvals; _audit = audit; }

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var items = await _db.LoanApplications.AsNoTracking()
            .Where(a => a.Status == LoanApplicationStatus.Recommended)
            .OrderBy(a => a.SubmittedAt)
            .Select(a => new DecisionRow(a.Id, a.ApplicationNumber,
                a.Borrower.FirstName + " " + a.Borrower.LastName, a.LoanProduct.ProductName,
                a.RequestedAmount, a.SubmittedAt,
                a.Approvals.Count(x => x.Decision == ApprovalDecision.Approved)))
            .ToListAsync();
        return View(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Case(Guid id)
    {
        var app = await _db.LoanApplications.AsNoTracking()
            .Include(a => a.Borrower).Include(a => a.LoanProduct)
            .FirstOrDefaultAsync(a => a.Id == id);
        if (app is null) return NotFound();

        await _audit.LogAsync("DecisionCaseViewed", "LoanApplication", id.ToString());

        var assessment = await _db.CreditAssessments.AsNoTracking()
            .Where(a => a.LoanApplicationId == id && a.IsCurrent)
            .OrderByDescending(a => a.AssessedAt).FirstOrDefaultAsync();

        var policy = await _db.CreditPolicyVersions.AsNoTracking().FirstOrDefaultAsync(p => p.IsActive);
        var approvals = await _db.LoanApprovals.AsNoTracking()
            .Where(a => a.LoanApplicationId == id).OrderBy(a => a.ApprovalLevel)
            .Select(a => new ApprovalRow(a.ApprovalLevel, a.Decision, a.ApprovedAmount, a.Conditions,
                a.Comments, a.DecidedAt, a.ReviewedByUser!.FirstName + " " + a.ReviewedByUser.LastName,
                a.ReviewedByUserId))
            .ToListAsync();

        var employment = await _db.EmploymentVerifications.AsNoTracking()
            .Where(v => v.LoanApplicationId == id).OrderByDescending(v => v.AttemptedAt)
            .Select(v => new { v.Outcome, v.ConfirmedNetSalary, v.Notes }).FirstOrDefaultAsync();

        return View(new DecisionCase
        {
            Id = app.Id,
            Number = app.ApplicationNumber,
            Status = app.Status,
            Borrower = $"{app.Borrower.FirstName} {app.Borrower.LastName}".Trim(),
            KycStatus = app.Borrower.KycStatus.ToString(),
            Product = app.LoanProduct.ProductName,
            Amount = app.RequestedAmount,
            Term = app.RequestedTermPeriods,
            Purpose = app.LoanPurpose,
            Assessment = assessment is null ? null : new AssessmentSummary(assessment.Recommendation,
                assessment.MonthlyIncome, assessment.ProposedMonthlyInstallment, assessment.DebtServiceRatio,
                assessment.DisposableIncome, assessment.RiskFlags, assessment.Notes,
                assessment.AssessedAmount != app.RequestedAmount || assessment.AssessedTermPeriods != app.RequestedTermPeriods),
            EmploymentOutcome = employment?.Outcome.ToString() ?? "None",
            EmploymentNotes = employment?.Notes,
            DualRequired = policy?.DualApprovalAbove is { } l && app.RequestedAmount > l,
            Approvals = approvals,
            CanDecide = app.Status == LoanApplicationStatus.Recommended
                        && app.Borrower.ApplicationUserId != UserId
                        && approvals.All(a => a.ManagerId != UserId)
        });
    }

    [HttpPost("{id:guid}/decide")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Decide(Guid id, ApprovalAction action, decimal? approvedAmount,
        string? conditions, string? reason)
    {
        var r = await _approvals.DecideAsync(id, action, approvedAmount, conditions, reason, UserId);
        TempData[r.Succeeded ? "Success" : "Error"] = r.Succeeded ? r.Message : r.Error;
        return r.Succeeded && action == ApprovalAction.Decline || (r.Succeeded && r.Message!.StartsWith("Application"))
            ? RedirectToAction(nameof(Index))
            : RedirectToAction(nameof(Case), new { id });
    }

    public record DecisionRow(Guid Id, string Number, string Borrower, string Product,
        decimal Amount, DateTime? SubmittedAt, int Approvals);

    public record ApprovalRow(int Level, ApprovalDecision Decision, decimal? Amount, string? Conditions,
        string? Comments, DateTime At, string By, string ManagerId);

    public record AssessmentSummary(CreditRecommendation Recommendation, decimal Income, decimal Instalment,
        decimal Ratio, decimal Disposable, string? Flags, string? Notes, bool Stale);

    public class DecisionCase
    {
        public Guid Id { get; set; }
        public string Number { get; set; } = "";
        public LoanApplicationStatus Status { get; set; }
        public string Borrower { get; set; } = "";
        public string KycStatus { get; set; } = "";
        public string Product { get; set; } = "";
        public decimal Amount { get; set; }
        public int Term { get; set; }
        public string Purpose { get; set; } = "";
        public AssessmentSummary? Assessment { get; set; }
        public string EmploymentOutcome { get; set; } = "";
        public string? EmploymentNotes { get; set; }
        public bool DualRequired { get; set; }
        public List<ApprovalRow> Approvals { get; set; } = new();
        public bool CanDecide { get; set; }
    }
}