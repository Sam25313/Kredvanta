using System.Security.Claims;
using Kredvanta.Authorization;
using Kredvanta.Data;
using Kredvanta.Services.Audit;
using Kredvanta.Services.Repayments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static Kredvanta.Models.Enums.LendingEnums;

namespace Kredvanta.Controllers;

[Authorize(Roles = AppRoles.FinanceOfficer)]
[Route("staff/repayments")]
public class StaffRepaymentsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IRepaymentService _repayments;
    private readonly IAuditService _audit;

    public StaffRepaymentsController(ApplicationDbContext db, IRepaymentService r, IAuditService a)
    { _db = db; _repayments = r; _audit = a; }

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet("")]
    public async Task<IActionResult> Index(string? q, bool overdueOnly = false)
    {
        var loans = _db.Loans.AsNoTracking().Where(l => l.Status == LoanStatus.Active || l.Status == LoanStatus.Overdue);
        if (overdueOnly) loans = loans.Where(l => l.Status == LoanStatus.Overdue);
        if (!string.IsNullOrWhiteSpace(q))
            loans = loans.Where(l => l.LoanNumber.Contains(q) || l.Borrower.BorrowerNumber.Contains(q) ||
                                     l.Borrower.LastName.Contains(q) || l.Borrower.PhoneNumber.Contains(q));

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var rows = await loans.OrderBy(l => l.Status == LoanStatus.Overdue ? 0 : 1).ThenBy(l => l.LoanNumber).Take(100)
            .Select(l => new LoanRow(l.Id, l.LoanNumber, l.Borrower.FirstName + " " + l.Borrower.LastName,
                l.OutstandingBalance, l.Status,
                l.RepaymentSchedules.Where(s => s.AmountPaid < s.TotalDue && s.DueDate < today).Count(),
                l.RepaymentSchedules.Where(s => s.AmountPaid < s.TotalDue).OrderBy(s => s.DueDate)
                    .Select(s => (DateOnly?)s.DueDate).FirstOrDefault()))
            .ToListAsync();

        ViewBag.Q = q; ViewBag.OverdueOnly = overdueOnly;
        return View(rows);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Loan(Guid id)
    {
        var loan = await _db.Loans.AsNoTracking().Include(l => l.Borrower)
            .Include(l => l.RepaymentSchedules).FirstOrDefaultAsync(l => l.Id == id);
        if (loan is null) return NotFound();

        await _audit.LogAsync("LoanViewedByFinance", nameof(Models.Loans.Loan), id.ToString());

        var payments = await _db.Repayments.AsNoTracking().Where(p => p.LoanId == id)
            .OrderByDescending(p => p.ReceivedAt).ToListAsync();

        return View(new LoanPage
        {
            Loan = loan,
            Payments = payments,
            IdempotencyKey = Guid.NewGuid().ToString("N"),
            Schedule = loan.RepaymentSchedules.OrderBy(s => s.InstallmentNumber).ToList(),
            CanRecord = loan.Status is LoanStatus.Active or LoanStatus.Overdue
                        && loan.Borrower.ApplicationUserId != UserId
        });
    }

    [HttpPost("{id:guid}/pay")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Pay(Guid id, RecordPaymentRequest request)
    {
        request.LoanId = id;
        var r = await _repayments.RecordAsync(request, UserId);
        TempData[r.Succeeded ? "Success" : "Error"] = r.Succeeded ? r.Message : r.Error;
        return RedirectToAction(nameof(Loan), new { id });
    }

    [HttpPost("{id:guid}/reverse/{repaymentId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reverse(Guid id, Guid repaymentId, string? reason)
    {
        var r = await _repayments.ReverseAsync(repaymentId, reason ?? "", UserId);
        TempData[r.Succeeded ? "Success" : "Error"] = r.Succeeded ? r.Message : r.Error;
        return RedirectToAction(nameof(Loan), new { id });
    }

    public record LoanRow(Guid Id, string Number, string Borrower, decimal Balance, LoanStatus Status,
        int OverdueInstalments, DateOnly? NextDue);

    public class LoanPage
    {
        public Kredvanta.Models.Loans.Loan Loan { get; set; } = null!;
        public List<Kredvanta.Models.Repayments.RepaymentSchedule> Schedule { get; set; } = new();
        public List<Kredvanta.Models.Repayments.Repayment> Payments { get; set; } = new();
        public string IdempotencyKey { get; set; } = "";
        public bool CanRecord { get; set; }
    }
}