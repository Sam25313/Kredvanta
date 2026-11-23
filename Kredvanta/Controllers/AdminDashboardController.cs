using Kredvanta.Authorization;
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
    [Authorize(Roles = AppRoles.Administrator)]
    public class AdminDashboardController : Controller
    {
        // Roles whose absence would stall a stage of the lending process.
        private static readonly string[] CoverageRoles =
        {
            AppRoles.LoanOfficer, AppRoles.KycOfficer, AppRoles.VerificationOfficer,
            AppRoles.CreditAnalyst, AppRoles.CreditManager
        };

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

            // 1. ACCOUNTS
            var roleRows = await (from ur in _context.UserRoles
                                  join r in _context.Roles on ur.RoleId equals r.Id
                                  join u in _context.Users on ur.UserId equals u.Id
                                  select new { u.Id, u.IsActive, Role = r.Name })
                                 .AsNoTracking().ToListAsync();

            var staffRows = roleRows.Where(x => x.Role != AppRoles.Borrower).ToList();

            model.TotalUsers = await _userManager.Users.CountAsync();
            model.ActiveUsers = await _userManager.Users.CountAsync(u => u.IsActive);
            model.TotalBorrowers = await _context.Borrowers.CountAsync();
            model.BorrowerAccounts = roleRows.Where(x => x.Role == AppRoles.Borrower)
                                             .Select(x => x.Id).Distinct().Count();

            model.StaffCount = staffRows.Select(x => x.Id).Distinct().Count();
            model.ActiveStaffCount = staffRows.Where(x => x.IsActive).Select(x => x.Id).Distinct().Count();
            model.InactiveStaffCount = model.StaffCount - model.ActiveStaffCount;

            model.RoleCoverage = CoverageRoles
                .Select(role => new RoleCoverageItem
                {
                    Role = StaffRoleLabels.Friendly(role),
                    ActiveUsers = staffRows.Where(x => x.IsActive && x.Role == role)
                                           .Select(x => x.Id).Distinct().Count()
                })
                .ToList();
            model.HasActiveCreditManager = staffRows.Any(x => x.IsActive && x.Role == AppRoles.CreditManager);

            // 2. PRODUCTS
            model.TotalLoanProducts = await _context.LoanProducts.CountAsync();
            model.ActiveLoanProducts = await _context.LoanProducts.CountAsync(p => p.IsActive);
            model.AvailableLoanProducts = await _context.LoanProducts
                .CountAsync(p => p.IsActive && p.IsAvailableForApplication);
            model.UnavailableLoanProducts = await _context.LoanProducts
                .CountAsync(p => p.IsActive && !p.IsAvailableForApplication);

            // 3. APPLICATIONS (counts only; case work happens in the staff workspace)
            model.TotalApplications = await _context.LoanApplications.CountAsync();

            model.PendingApplications = await _context.LoanApplications.CountAsync(a =>
                a.Status == LoanApplicationStatus.Submitted ||
                a.Status == LoanApplicationStatus.UnderReview ||
                a.Status == LoanApplicationStatus.DocumentsRequired ||
                a.Status == LoanApplicationStatus.Recommended);

            model.ApplicationsUnderReview = await _context.LoanApplications
                .CountAsync(a => a.Status == LoanApplicationStatus.UnderReview);
            model.ApprovedApplications = await _context.LoanApplications
                .CountAsync(a => a.Status == LoanApplicationStatus.Approved);
            model.RejectedApplications = await _context.LoanApplications
                .CountAsync(a => a.Status == LoanApplicationStatus.Rejected);

            model.RecentApplications = await _context.LoanApplications
                .AsNoTracking()
                .OrderByDescending(a => a.SubmittedAt ?? a.CreatedAt)
                .Take(5)
                .Select(a => new AdminRecentApplicationViewModel
                {
                    ApplicationNumber = a.ApplicationNumber,
                    BorrowerName = a.Borrower.FirstName + " " + a.Borrower.LastName,
                    ProductName = a.LoanProduct.ProductName,
                    RequestedAmount = a.RequestedAmount,
                    Status = a.Status,
                    SubmittedAt = a.SubmittedAt
                })
                .ToListAsync();

            model.RecentRepayments = await _context.Repayments
                .AsNoTracking()
                .OrderByDescending(r => r.ReceivedAt)
                .Take(5)
                .Select(r => new AdminRecentRepaymentViewModel
                {
                    ReceiptNumber = r.ReceiptNumber,
                    LoanNumber = r.Loan.LoanNumber,
                    CurrencyCode = r.Loan.CurrencyCode,
                    Amount = r.Amount,
                    Status = r.Status,
                    ReceivedAt = r.ReceivedAt
                })
                .ToListAsync();

            // 4. PORTFOLIO
            model.TotalLoans = await _context.Loans.CountAsync();
            model.ActiveLoans = await _context.Loans.CountAsync(l => l.Status == LoanStatus.Active);
            model.OverdueLoans = await _context.Loans.CountAsync(l => l.Status == LoanStatus.Overdue);

            model.TotalDisbursedPrincipal = await _context.Loans
                .Where(l => l.DisbursementDate != null)
                .SumAsync(l => (decimal?)l.PrincipalAmount) ?? 0m;

            model.TotalOutstandingBalance = await _context.Loans
                .Where(l => l.DisbursementDate != null)
                .SumAsync(l => (decimal?)l.OutstandingBalance) ?? 0m;

            model.TotalConfirmedRepayments = await _context.Repayments
                .Where(r => r.Status == RepaymentStatus.Confirmed)
                .SumAsync(r => (decimal?)r.Amount) ?? 0m;

            model.TotalRepayments = await _context.Repayments.CountAsync();
            model.PendingRepayments = await _context.Repayments
                .CountAsync(r => r.Status == RepaymentStatus.Pending);

            // 5. SETUP HEALTH
            var policy = await _context.CreditPolicyVersions.AsNoTracking()
                .FirstOrDefaultAsync(p => p.IsActive);

            model.HasActiveCreditPolicy = policy is not null;
            model.ActivePolicyName = policy?.Name;
            model.ActivePolicyVersion = policy?.VersionNumber;

            if (policy is null)
                model.SetupWarnings.Add(
                    "No credit policy is active. Analysts cannot assess applications until a credit manager's approved policy is activated.");
            else if (policy.Name.Contains("PLACEHOLDER", StringComparison.OrdinalIgnoreCase))
                model.SetupWarnings.Add(
                    "The active credit policy is a development placeholder. Replace it with an approved policy before real lending.");

            foreach (var c in model.RoleCoverage.Where(c => c.ActiveUsers == 0))
                model.SetupWarnings.Add(
                    $"No active {c.Role.ToLowerInvariant()}. Applications will stall at that stage.");

            var openWithoutDocs = await _context.LoanProducts.CountAsync(p =>
                p.IsActive && p.IsAvailableForApplication &&
                !p.RequiredDocuments.Any(d => d.IsActive && d.IsMandatory));
            if (openWithoutDocs > 0)
                model.SetupWarnings.Add(
                    $"{openWithoutDocs} product(s) accept applications but have no mandatory documents configured.");

            if (model.AvailableLoanProducts == 0)
                model.SetupWarnings.Add("No product is open for applications. Applicants cannot apply.");

            return View(model);
        }
    }
}