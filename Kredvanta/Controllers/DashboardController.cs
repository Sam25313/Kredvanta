
using Kredvanta.Data;
using Kredvanta.Models.Identity;
using Kredvanta.Models.Enums;
using Kredvanta.ViewModels.Dashboard;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using static Kredvanta.Models.Enums.LendingEnums;

namespace Kredvanta.Controllers
{
    [Authorize(Roles = "Borrower")]
    [Route("BorrowerDashboard")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DashboardController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            // 1. Identify the logged-in user.
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(userId))
                return Challenge();

            // 2. Retrieve the borrower's profile.
            var borrower = await _context.Borrowers
                .AsNoTracking()
                .FirstOrDefaultAsync(b =>
                    b.ApplicationUserId == userId &&
                    b.IsActive);

            // If the borrower has no profile, redirect them
            // to create one before accessing the dashboard.
            if (borrower == null)
            {
                return RedirectToAction("Step1", "Profile");
            }

            var model = new BorrowerDashboardViewModel
            {
                BorrowerName =
                    $"{borrower.FirstName} {borrower.LastName}".Trim(),

                BorrowerNumber = borrower.BorrowerNumber,
                KycStatus = borrower.KycStatus
            };

            // 3. Application statistics.
            var applications = _context.LoanApplications
                .AsNoTracking()
                .Where(a => a.BorrowerId == borrower.Id);

            model.TotalApplications =
                await applications.CountAsync();

            model.PendingApplications =
                await applications.CountAsync(a =>
                    a.Status == LoanApplicationStatus.Submitted ||
                    a.Status == LoanApplicationStatus.UnderReview ||
                    a.Status == LoanApplicationStatus.DocumentsRequired ||
                    a.Status == LoanApplicationStatus.Recommended);

            model.ApplicationsRequiringAction =
                await applications.CountAsync(a =>
                    a.Status == LoanApplicationStatus.DocumentsRequired);

            model.ApprovedApplications =
                await applications.CountAsync(a =>
                    a.Status == LoanApplicationStatus.Approved);

            model.RejectedApplications =
                await applications.CountAsync(a =>
                    a.Status == LoanApplicationStatus.Rejected);

            // 4. Retrieve the five most recent applications.
            model.RecentApplications = await applications
                .OrderByDescending(a => a.SubmittedAt)
                .ThenByDescending(a => a.CreatedAt)
                .Take(5)
                .Select(a =>
                    new BorrowerDashboardViewModel
                        .ApplicationSummaryViewModel
                    {
                        Id = a.Id,
                        ApplicationNumber = a.ApplicationNumber,
                        LoanProductName = a.LoanProduct.ProductName,
                        RequestedAmount = a.RequestedAmount,
                        RequestedTermPeriods = a.RequestedTermPeriods,
                        LoanPurpose = a.LoanPurpose,
                        Status = a.Status,
                        SubmittedAt = a.SubmittedAt,
                        DecisionAt = a.DecisionAt
                    })
                .ToListAsync();

            // 5. Retrieve the borrower's loans.
            var loans = await _context.Loans
                .AsNoTracking()
                .Where(l => l.BorrowerId == borrower.Id)
                .Include(l => l.RepaymentSchedules)
                .Include(l => l.Repayments)
                .OrderByDescending(l => l.DisbursementDate)
                .ToListAsync();

            // Exclude loans not yet disbursed and cancelled loans.
            var disbursedLoans = loans
                .Where(l =>
                    l.Status != LoanStatus.PendingDisbursement &&
                    l.Status != LoanStatus.Cancelled)
                .ToList();

            model.ActiveLoans = disbursedLoans.Count(l =>
                l.Status == LoanStatus.Active ||
                l.Status == LoanStatus.Overdue ||
                l.Status == LoanStatus.Restructured);

            model.TotalBorrowed = disbursedLoans
                .Sum(l => l.PrincipalAmount);

            model.TotalOutstanding = disbursedLoans
                .Sum(l => l.OutstandingBalance);

            model.TotalConfirmedRepayments = disbursedLoans
                .SelectMany(l => l.Repayments)
                .Where(r => r.Status == RepaymentStatus.Confirmed)
                .Sum(r => r.Amount);

            // 6. Prepare individual loan summaries.
            foreach (var loan in loans)
            {
                var confirmedRepayments = loan.Repayments
                    .Where(r => r.Status == RepaymentStatus.Confirmed)
                    .Sum(r => r.Amount);

                var progress = loan.TotalRepayable > 0
                    ? (confirmedRepayments / loan.TotalRepayable) * 100
                    : 0;

                // Use the server's local date for overdue calculations.
                var today = DateOnly.FromDateTime(DateTime.Today);

                var unpaidSchedules = loan.RepaymentSchedules
                    .Where(s =>
                        s.Status != ScheduleStatus.Paid &&
                        s.TotalDue > s.AmountPaid)
                    .OrderBy(s => s.DueDate)
                    .ToList();

                var nextInstallment = unpaidSchedules.FirstOrDefault();

                var summary =
                    new BorrowerDashboardViewModel.LoanSummaryViewModel
                    {
                        LoanNumber = loan.LoanNumber,
                        ProductName = loan.ProductNameSnapshot,
                        PrincipalAmount = loan.PrincipalAmount,
                        TotalRepayable = loan.TotalRepayable,
                        OutstandingBalance = loan.OutstandingBalance,
                        Status = loan.Status,
                        DisbursementDate = loan.DisbursementDate,
                        MaturityDate = loan.MaturityDate,

                        ConfirmedRepayments = confirmedRepayments,

                        RepaymentProgress = Math.Clamp(progress, 0, 100),

                        NextInstallmentAmount = nextInstallment == null
                            ? 0
                            : Math.Max(
                                0,
                                nextInstallment.TotalDue -
                                nextInstallment.AmountPaid),

                        NextInstallmentDate = nextInstallment?.DueDate,

                        OverdueInstallments = unpaidSchedules.Count(s =>
                            s.DueDate < today)
                    };

                if (loan.Status != LoanStatus.Cancelled)
                {
                    model.Loans.Add(summary);
                }
            }

            // 7. Retrieve recent repayment transactions.
            model.RecentRepayments = await _context.Repayments
                .AsNoTracking()
                .Where(r => r.Loan.BorrowerId == borrower.Id)
                .OrderByDescending(r => r.ReceivedAt)
                .Take(5)
                .Select(r =>
                    new BorrowerDashboardViewModel
                        .RecentRepaymentViewModel
                    {
                        ReceiptNumber = r.ReceiptNumber,
                        Amount = r.Amount,
                        Method = r.Method,
                        ReceivedAt = r.ReceivedAt,
                        Status = r.Status
                    })
                .ToListAsync();

            // 8. Retrieve available loan products.
            model.AvailableProducts = await _context.LoanProducts
                .AsNoTracking()
                .Where(p => p.IsActive)
                .OrderBy(p => p.Category)
                .ThenBy(p => p.ProductName)
                .Take(4)
                .ToListAsync();

            model.AvailableLoanProducts =
                await _context.LoanProducts
                    .CountAsync(p => p.IsActive);

            return View(model);
        }
    }
}
