using Kredvanta.Data;
using Kredvanta.Models.Identity;
using Kredvanta.ViewModels.Dashboard;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using static Kredvanta.Models.Enums.LendingEnums;

namespace Kredvanta.Controllers
{
    [Authorize(Roles = "Administrator")]
    public class AdminDashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminDashboardController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var model = new AdminDashboardViewModel();

            // -----------------------------------------
            // 1. USER STATISTICS
            // -----------------------------------------

            model.TotalUsers = await _userManager.Users
                .CountAsync();

            model.ActiveUsers = await _userManager.Users
                .CountAsync(u => u.IsActive);

            model.TotalBorrowers = await _context.Borrowers
                .CountAsync();


            // -----------------------------------------
            // 2. LOAN PRODUCT STATISTICS
            // -----------------------------------------

            model.TotalLoanProducts = await _context.LoanProducts
                .CountAsync();

            model.ActiveLoanProducts = await _context.LoanProducts
                .CountAsync(p => p.IsActive);

            model.AvailableLoanProducts = await _context.LoanProducts
                .CountAsync(p =>
                    p.IsActive &&
                    p.IsAvailableForApplication);

            model.UnavailableLoanProducts = await _context.LoanProducts
                .CountAsync(p =>
                    p.IsActive &&
                    !p.IsAvailableForApplication);


            // -----------------------------------------
            // 3. LOAN APPLICATION STATISTICS
            // -----------------------------------------

            model.TotalApplications = await _context.LoanApplications
                .CountAsync();

            model.PendingApplications = await _context.LoanApplications
                .CountAsync(a =>
                    a.Status == LoanApplicationStatus.Submitted ||
                    a.Status == LoanApplicationStatus.UnderReview ||
                    a.Status == LoanApplicationStatus.DocumentsRequired ||
                    a.Status == LoanApplicationStatus.Recommended);

            model.ApplicationsUnderReview =
                await _context.LoanApplications
                    .CountAsync(a =>
                        a.Status == LoanApplicationStatus.UnderReview);

            model.ApprovedApplications =
                await _context.LoanApplications
                    .CountAsync(a =>
                        a.Status == LoanApplicationStatus.Approved);

            model.RejectedApplications =
                await _context.LoanApplications
                    .CountAsync(a =>
                        a.Status == LoanApplicationStatus.Rejected);


            // -----------------------------------------
            // 4. LOAN STATISTICS
            // -----------------------------------------

            model.TotalLoans = await _context.Loans
                .CountAsync();

            model.ActiveLoans = await _context.Loans
                .CountAsync(l => l.Status == LoanStatus.Active);

            model.OverdueLoans = await _context.Loans
                .CountAsync(l => l.Status == LoanStatus.Overdue);


            // -----------------------------------------
            // 5. FINANCIAL STATISTICS
            // -----------------------------------------

            // Count only loans with a recorded
            // disbursement date as disbursed loans.

            model.TotalDisbursedPrincipal =
                await _context.Loans
                    .Where(l => l.DisbursementDate != null)
                    .SumAsync(l => (decimal?)l.PrincipalAmount)
                    ?? 0m;

            model.TotalOutstandingBalance =
                await _context.Loans
                    .Where(l => l.DisbursementDate != null)
                    .SumAsync(l => (decimal?)l.OutstandingBalance)
                    ?? 0m;

            // Only confirmed repayments count as
            // successfully collected repayments.

            model.TotalConfirmedRepayments =
                await _context.Repayments
                    .Where(r => r.Status == RepaymentStatus.Confirmed)
                    .SumAsync(r => (decimal?)r.Amount)
                    ?? 0m;


            // -----------------------------------------
            // 6. REPAYMENT STATISTICS
            // -----------------------------------------

            model.TotalRepayments = await _context.Repayments
                .CountAsync();

            model.PendingRepayments = await _context.Repayments
                .CountAsync(r =>
                    r.Status == RepaymentStatus.Pending);


            // -----------------------------------------
            // 7. RECENT LOAN APPLICATIONS
            // -----------------------------------------

            model.RecentApplications =
                await _context.LoanApplications
                    .AsNoTracking()
                    .OrderByDescending(a =>
                        a.SubmittedAt ?? a.CreatedAt)
                    .Take(8)
                    .Select(a => new AdminRecentApplicationViewModel
                    {
                        ApplicationNumber = a.ApplicationNumber,

                        BorrowerName =
                            a.Borrower.FirstName + " " +
                            a.Borrower.LastName,

                        ProductName = a.LoanProduct.ProductName,

                        RequestedAmount = a.RequestedAmount,

                        Status = a.Status,

                        SubmittedAt = a.SubmittedAt
                    })
                    .ToListAsync();


            // -----------------------------------------
            // 8. RECENT REPAYMENTS
            // -----------------------------------------

            model.RecentRepayments =
                await _context.Repayments
                    .AsNoTracking()
                    .OrderByDescending(r => r.ReceivedAt)
                    .Take(8)
                    .Select(r => new AdminRecentRepaymentViewModel
                    {
                        ReceiptNumber = r.ReceiptNumber,

                        LoanNumber = r.Loan.LoanNumber,

                        Amount = r.Amount,

                        Status = r.Status,

                        ReceivedAt = r.ReceivedAt,

                        CurrencyCode = r.Loan.CurrencyCode
                    })
                    .ToListAsync();


            return View(model);
        }
    }
}