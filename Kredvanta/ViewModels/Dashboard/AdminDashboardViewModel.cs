using static Kredvanta.Models.Enums.LendingEnums;

namespace Kredvanta.ViewModels.Dashboard
{
    public class AdminRecentApplicationViewModel
    {
        public string ApplicationNumber { get; set; } = "";
        public string BorrowerName { get; set; } = "";
        public string ProductName { get; set; } = "";
        public decimal RequestedAmount { get; set; }
        public LoanApplicationStatus Status { get; set; }
        public DateTime? SubmittedAt { get; set; }
    }

    public class AdminRecentRepaymentViewModel
    {
        public string ReceiptNumber { get; set; } = "";
        public string LoanNumber { get; set; } = "";
        public string CurrencyCode { get; set; } = "KES";
        public decimal Amount { get; set; }
        public RepaymentStatus Status { get; set; }
        public DateTime ReceivedAt { get; set; }
    }

    public class RoleCoverageItem
    {
        public string Role { get; set; } = "";
        public int ActiveUsers { get; set; }
    }

    public class AdminDashboardViewModel
    {
        // Accounts
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int BorrowerAccounts { get; set; }
        public int TotalBorrowers { get; set; }
        public int StaffCount { get; set; }
        public int ActiveStaffCount { get; set; }
        public int InactiveStaffCount { get; set; }

        // Products
        public int TotalLoanProducts { get; set; }
        public int ActiveLoanProducts { get; set; }
        public int AvailableLoanProducts { get; set; }
        public int UnavailableLoanProducts { get; set; }

        // Applications (counts only)
        public int TotalApplications { get; set; }
        public int PendingApplications { get; set; }
        public int ApplicationsUnderReview { get; set; }
        public int ApprovedApplications { get; set; }
        public int RejectedApplications { get; set; }

        // Portfolio
        public int TotalLoans { get; set; }
        public int ActiveLoans { get; set; }
        public int OverdueLoans { get; set; }
        public decimal TotalDisbursedPrincipal { get; set; }
        public decimal TotalOutstandingBalance { get; set; }
        public decimal TotalConfirmedRepayments { get; set; }
        public int TotalRepayments { get; set; }
        public int PendingRepayments { get; set; }

        // Setup health
        public bool HasActiveCreditPolicy { get; set; }
        public bool HasActiveCreditManager { get; set; }
        public string? ActivePolicyName { get; set; }
        public int? ActivePolicyVersion { get; set; }
        public List<RoleCoverageItem> RoleCoverage { get; set; } = new();
        public List<string> SetupWarnings { get; set; } = new();

        // Kept so existing code compiles; no longer shown to administrators.
        public List<AdminRecentApplicationViewModel> RecentApplications { get; set; } = new();
        public List<AdminRecentRepaymentViewModel> RecentRepayments { get; set; } = new();
    }
}