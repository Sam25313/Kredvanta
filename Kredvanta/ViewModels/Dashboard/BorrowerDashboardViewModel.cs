using Kredvanta.Models.Enums;
using Kredvanta.Models.Products;
using static Kredvanta.Models.Enums.LendingEnums;

namespace Kredvanta.ViewModels.Dashboard
{
    public class BorrowerDashboardViewModel
    {
        // Borrower information
        public string BorrowerName { get; set; } = string.Empty;
        public string BorrowerNumber { get; set; } = string.Empty;
        public KycStatus KycStatus { get; set; }

        // Loan application statistics
        public int TotalApplications { get; set; }
        public int PendingApplications { get; set; }
        public int ApprovedApplications { get; set; }
        public int RejectedApplications { get; set; }
        public int ApplicationsRequiringAction { get; set; }

        // Loan financial summary
        public int ActiveLoans { get; set; }
        public decimal TotalBorrowed { get; set; }
        public decimal TotalOutstanding { get; set; }
        public decimal TotalConfirmedRepayments { get; set; }

        // Loan product catalogue
        public int AvailableLoanProducts { get; set; }
        public List<LoanProduct> AvailableProducts { get; set; } = new();

        // Guided journey (NEW)
        public BorrowerJourneyViewModel Journey { get; set; } = new();

        // Dashboard collections
        public List<ApplicationSummaryViewModel> RecentApplications { get; set; } = new();
        public List<LoanSummaryViewModel> Loans { get; set; } = new();
        public List<RecentRepaymentViewModel> RecentRepayments { get; set; } = new();

        public class ApplicationSummaryViewModel
        {
            public Guid Id { get; set; }
            public string ApplicationNumber { get; set; } = string.Empty;
            public string LoanProductName { get; set; } = string.Empty;
            public decimal RequestedAmount { get; set; }
            public int RequestedTermPeriods { get; set; }
            public string LoanPurpose { get; set; } = string.Empty;
            public LoanApplicationStatus Status { get; set; }
            public DateTime? SubmittedAt { get; set; }
            public DateTime? DecisionAt { get; set; }
            public int ProgressPercentage { get; set; }
            public string ProgressMessage { get; set; } = string.Empty;
            public List<ApplicationHistoryViewModel> History { get; set; } = new();
        }

        public class ApplicationHistoryViewModel
        {
            public LoanApplicationStatus? PreviousStatus { get; set; }
            public LoanApplicationStatus NewStatus { get; set; }
            public DateTime ChangedAt { get; set; }
            public string? Notes { get; set; }
            public string? ChangedByName { get; set; }
        }

        public class LoanSummaryViewModel
        {
            public Guid Id  { get; set; }
            public string LoanNumber { get; set; } = string.Empty;
            public string ProductName { get; set; } = string.Empty;
            public decimal PrincipalAmount { get; set; }
            public decimal TotalRepayable { get; set; }
            public decimal OutstandingBalance { get; set; }
            public LoanStatus Status { get; set; }
            public DateOnly? DisbursementDate { get; set; }
            public DateOnly? MaturityDate { get; set; }
            public decimal ConfirmedRepayments { get; set; }
            public decimal RepaymentProgress { get; set; }
            public decimal NextInstallmentAmount { get; set; }
            public DateOnly? NextInstallmentDate { get; set; }
            public int OverdueInstallments { get; set; }
        }

        public class RecentRepaymentViewModel
        {
            public string ReceiptNumber { get; set; } = string.Empty;
            public decimal Amount { get; set; }
            public RepaymentMethod Method { get; set; }
            public DateTime ReceivedAt { get; set; }
            public RepaymentStatus Status { get; set; }
        }
    }
}