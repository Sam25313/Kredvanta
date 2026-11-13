using Kredvanta.Models.Enums;
using static Kredvanta.Models.Enums.LendingEnums;

namespace Kredvanta.ViewModels.Dashboard
{
    public class AdminDashboardViewModel
    {
        // User statistics
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int TotalBorrowers { get; set; }

        // Loan product statistics
        public int TotalLoanProducts { get; set; }

        public int ActiveLoanProducts { get; set; }

        public int AvailableLoanProducts { get; set; }

        public int UnavailableLoanProducts { get; set; }

        // Application statistics
        public int TotalApplications { get; set; }
        public int PendingApplications { get; set; }
        public int ApplicationsUnderReview { get; set; }
        public int ApprovedApplications { get; set; }
        public int RejectedApplications { get; set; }

        // Loan statistics
        public int TotalLoans { get; set; }
        public int ActiveLoans { get; set; }
        public int OverdueLoans { get; set; }

        // Financial statistics
        public decimal TotalDisbursedPrincipal { get; set; }
        public decimal TotalOutstandingBalance { get; set; }
        public decimal TotalConfirmedRepayments { get; set; }

        // Repayment statistics
        public int TotalRepayments { get; set; }
        public int PendingRepayments { get; set; }

        // Recent records
        public List<AdminRecentApplicationViewModel> RecentApplications
        {
            get; set;
        } = new();

        public List<AdminRecentRepaymentViewModel> RecentRepayments
        {
            get; set;
        } = new();
    }

    public class AdminRecentApplicationViewModel
    {
        public string ApplicationNumber { get; set; } = string.Empty;

        public string BorrowerName { get; set; } = string.Empty;

        public string ProductName { get; set; } = string.Empty;

        public decimal RequestedAmount { get; set; }

        public LoanApplicationStatus Status { get; set; }

        public DateTime? SubmittedAt { get; set; }
    }

    public class AdminRecentRepaymentViewModel
    {
        public string ReceiptNumber { get; set; } = string.Empty;

        public string LoanNumber { get; set; } = string.Empty;

        public decimal Amount { get; set; }

        public RepaymentStatus Status { get; set; }

        public DateTime ReceivedAt { get; set; }

        public string CurrencyCode { get; set; } = "KES";
    }
}