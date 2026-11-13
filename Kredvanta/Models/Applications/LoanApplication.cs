
using Kredvanta.Models.Borrowers;
using Kredvanta.Models.Common;
using Kredvanta.Models.Enums;
using Kredvanta.Models.Identity;
using Kredvanta.Models.Loans;
using Kredvanta.Models.Products;
using static Kredvanta.Models.Enums.LendingEnums;

namespace Kredvanta.Models.Applications
{
    public class LoanApplication : BaseEntity
    {
        public string ApplicationNumber { get; set; } = string.Empty;

        public Guid BorrowerId { get; set; }

        public Borrower Borrower { get; set; } = null!;

        public Guid LoanProductId { get; set; }

        public LoanProduct LoanProduct { get; set; } = null!;

        public decimal RequestedAmount { get; set; }

        public int RequestedTermPeriods { get; set; }

        public string LoanPurpose { get; set; } = string.Empty;

        public LoanApplicationStatus Status { get; set; }
            = LoanApplicationStatus.Draft;

        // Staff member responsible for reviewing the application
        public string? AssignedOfficerId { get; set; }

        public ApplicationUser? AssignedOfficer { get; set; }

        public DateTime? SubmittedAt { get; set; }

        public DateTime? DecisionAt { get; set; }

        public string? CancellationReason { get; set; }

        // Relationships
        public ICollection<LoanApplicationDocument> Documents { get; set; }
            = new List<LoanApplicationDocument>();

        public ICollection<LoanApplicationGuarantor> Guarantors { get; set; }
            = new List<LoanApplicationGuarantor>();

        public ICollection<CreditAssessment> CreditAssessments { get; set; }
            = new List<CreditAssessment>();

        public ICollection<LoanApproval> Approvals { get; set; }
            = new List<LoanApproval>();
        public ICollection<LoanApplicationStatusHistory> StatusHistory { get; set; }
           = new List<LoanApplicationStatusHistory>();

        public Loan? Loan { get; set; }
    }
}
