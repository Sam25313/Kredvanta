using Kredvanta.Authorization;
using Kredvanta.Data;
using Kredvanta.Models.Identity;
using Kredvanta.Services.Journey;
using Kredvanta.ViewModels.Dashboard;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using static Kredvanta.Models.Enums.LendingEnums;

namespace Kredvanta.Controllers
{
    [Authorize(Roles = AppRoles.Borrower)]
    [Route("BorrowerDashboard")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IBorrowerJourneyService _journey;

        public DashboardController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IBorrowerJourneyService journey)
        {
            _context = context;
            _userManager = userManager;
            _journey = journey;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Challenge();

            var borrower = await _context.Borrowers
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.ApplicationUserId == userId && b.IsActive);

            if (borrower == null)
                return RedirectToAction("Step1", "Profile");

            var model = new BorrowerDashboardViewModel
            {
                BorrowerName = $"{borrower.FirstName} {borrower.LastName}".Trim(),
                BorrowerNumber = borrower.BorrowerNumber,
                KycStatus = borrower.KycStatus
            };

            // Application statistics
            var applications = _context.LoanApplications
                .AsNoTracking()
                .Where(a => a.BorrowerId == borrower.Id);

            model.TotalApplications = await applications.CountAsync();

            model.PendingApplications = await applications.CountAsync(a =>
                a.Status == LoanApplicationStatus.Submitted ||
                a.Status == LoanApplicationStatus.UnderReview ||
                a.Status == LoanApplicationStatus.DocumentsRequired ||
                a.Status == LoanApplicationStatus.Recommended);

            // Drafts also need action from the borrower (upload and submit).
            model.ApplicationsRequiringAction = await applications.CountAsync(a =>
                a.Status == LoanApplicationStatus.DocumentsRequired ||
                a.Status == LoanApplicationStatus.Draft);

            model.ApprovedApplications = await applications.CountAsync(a =>
                a.Status == LoanApplicationStatus.Approved);

            model.RejectedApplications = await applications.CountAsync(a =>
                a.Status == LoanApplicationStatus.Rejected);

            model.RecentApplications = await applications
                .OrderByDescending(a => a.SubmittedAt)
                .ThenByDescending(a => a.CreatedAt)
                .Take(5)
                .Select(a => new BorrowerDashboardViewModel.ApplicationSummaryViewModel
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

            // Loans
            var loans = await _context.Loans
                .AsNoTracking()
                .Where(l => l.BorrowerId == borrower.Id)
                .Include(l => l.RepaymentSchedules)
                .Include(l => l.Repayments)
                .OrderByDescending(l => l.DisbursementDate)
                .ToListAsync();

            var disbursedLoans = loans
                .Where(l => l.Status != LoanStatus.PendingDisbursement &&
                            l.Status != LoanStatus.Cancelled)
                .ToList();

            model.ActiveLoans = disbursedLoans.Count(l =>
                l.Status == LoanStatus.Active ||
                l.Status == LoanStatus.Overdue ||
                l.Status == LoanStatus.Restructured);

            model.TotalBorrowed = disbursedLoans.Sum(l => l.PrincipalAmount);
            model.TotalOutstanding = disbursedLoans.Sum(l => l.OutstandingBalance);

            model.TotalConfirmedRepayments = disbursedLoans
                .SelectMany(l => l.Repayments)
                .Where(r => r.Status == RepaymentStatus.Confirmed)
                .Sum(r => r.Amount);

            var today = DateOnly.FromDateTime(DateTime.Today);

            foreach (var loan in loans.Where(l => l.Status != LoanStatus.Cancelled))
            {
                var confirmed = loan.Repayments
                    .Where(r => r.Status == RepaymentStatus.Confirmed)
                    .Sum(r => r.Amount);

                var progress = loan.TotalRepayable > 0
                    ? confirmed / loan.TotalRepayable * 100
                    : 0;

                var unpaid = loan.RepaymentSchedules
                    .Where(s => s.Status != ScheduleStatus.Paid && s.TotalDue > s.AmountPaid)
                    .OrderBy(s => s.DueDate)
                    .ToList();

                var next = unpaid.FirstOrDefault();

                model.Loans.Add(new BorrowerDashboardViewModel.LoanSummaryViewModel
                {
                    Id = loan.Id,
                    LoanNumber = loan.LoanNumber,
                    ProductName = loan.ProductNameSnapshot,
                    PrincipalAmount = loan.PrincipalAmount,
                    TotalRepayable = loan.TotalRepayable,
                    OutstandingBalance = loan.OutstandingBalance,
                    Status = loan.Status,
                    DisbursementDate = loan.DisbursementDate,
                    MaturityDate = loan.MaturityDate,
                    ConfirmedRepayments = confirmed,
                    RepaymentProgress = Math.Clamp(progress, 0, 100),
                    NextInstallmentAmount = next == null ? 0 : Math.Max(0, next.TotalDue - next.AmountPaid),
                    NextInstallmentDate = next?.DueDate,
                    OverdueInstallments = unpaid.Count(s => s.DueDate < today)
                });
            }

            model.RecentRepayments = await _context.Repayments
                .AsNoTracking()
                .Where(r => r.Loan.BorrowerId == borrower.Id)
                .OrderByDescending(r => r.ReceivedAt)
                .Take(5)
                .Select(r => new BorrowerDashboardViewModel.RecentRepaymentViewModel
                {
                    ReceiptNumber = r.ReceiptNumber,
                    Amount = r.Amount,
                    Method = r.Method,
                    ReceivedAt = r.ReceivedAt,
                    Status = r.Status
                })
                .ToListAsync();

            model.AvailableProducts = await _context.LoanProducts
                .AsNoTracking()
                .Where(p => p.IsActive && p.IsAvailableForApplication)
                .OrderBy(p => p.Category)
                .ThenBy(p => p.ProductName)
                .Take(4)
                .ToListAsync();

            model.AvailableLoanProducts = await _context.LoanProducts.CountAsync(
                p => p.IsActive && p.IsAvailableForApplication);

            // Guided journey: profile, eligibility, apply, documents, submit
            model.Journey = await _journey.BuildAsync(userId);

            return View(model);
        }
    }
}