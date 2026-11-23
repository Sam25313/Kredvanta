using System.Security.Claims;
using Kredvanta.Authorization;
using Kredvanta.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static Kredvanta.Models.Enums.LendingEnums;

namespace Kredvanta.Controllers;

[Authorize(Roles = AppRoles.Borrower)]
[Route("my-loan/{id:guid}")]
public class BorrowerLoanController : Controller
{
    private readonly ApplicationDbContext _db;
    public BorrowerLoanController(ApplicationDbContext db) => _db = db;

    [HttpGet("")]
    public async Task<IActionResult> Statement(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // Ownership is part of the query: another borrower's loan simply isn't found.
        var loan = await _db.Loans.AsNoTracking().Include(l => l.RepaymentSchedules)
            .FirstOrDefaultAsync(l => l.Id == id && l.Borrower.ApplicationUserId == userId);
        if (loan is null) return NotFound();

        var payments = await _db.Repayments.AsNoTracking()
            .Where(p => p.LoanId == id && p.Status == RepaymentStatus.Confirmed)
            .OrderByDescending(p => p.ReceivedAt).ToListAsync();

        return View(new StatementPage
        {
            Loan = loan,
            Payments = payments,
            Schedule = loan.RepaymentSchedules.OrderBy(s => s.InstallmentNumber).ToList(),
            Paid = payments.Sum(p => p.Amount)
        });
    }

    public class StatementPage
    {
        public Kredvanta.Models.Loans.Loan Loan { get; set; } = null!;
        public List<Kredvanta.Models.Repayments.RepaymentSchedule> Schedule { get; set; } = new();
        public List<Kredvanta.Models.Repayments.Repayment> Payments { get; set; } = new();
        public decimal Paid { get; set; }
    }
}