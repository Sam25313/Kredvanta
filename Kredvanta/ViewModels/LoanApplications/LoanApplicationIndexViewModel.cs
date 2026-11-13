using static Kredvanta.Models.Enums.LendingEnums;

namespace Kredvanta.ViewModels.LoanApplications
{
    public class LoanApplicationIndexViewModel
    {
        public int TotalApplications { get; set; }

        public int PendingApplications { get; set; }

        public int ApprovedApplications { get; set; }

        public int RejectedApplications { get; set; }

        public List<LoanApplicationListItemViewModel> Applications { get; set; } = new();
    }

    public class LoanApplicationListItemViewModel
    {
        public Guid Id { get; set; }

        public string ApplicationNumber { get; set; } = string.Empty;

        public string LoanProductName { get; set; } = string.Empty;

        public decimal RequestedAmount { get; set; }

        public int RequestedTermPeriods { get; set; }

        public string LoanPurpose { get; set; } = string.Empty;

        public LoanApplicationStatus Status { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? SubmittedAt { get; set; }

        public DateTime? DecisionAt { get; set; }
    }
}