
using Kredvanta.Models.Applications;
using Kredvanta.Models.Borrowers;
using Kredvanta.Models.Common;
using Kredvanta.Models.Enums;
using Kredvanta.Models.Repayments;
using static Kredvanta.Models.Enums.LendingEnums;

namespace Kredvanta.Models.Loans
{
    public class Loan : BaseEntity
    {
        public string LoanNumber { get; set; } = string.Empty;

        // One approved application creates at most one loan.
        public Guid LoanApplicationId { get; set; }

        public LoanApplication LoanApplication { get; set; } = null!;

        public Guid BorrowerId { get; set; }

        public Borrower Borrower { get; set; } = null!;

        // Product details preserved as a snapshot
        public string ProductNameSnapshot { get; set; } = string.Empty;

        public string ProductCodeSnapshot { get; set; } = string.Empty;

        // Approved financial terms
        public decimal PrincipalAmount { get; set; }

        public decimal AnnualInterestRate { get; set; }

        public InterestMethod InterestMethod { get; set; }

        public RepaymentFrequency RepaymentFrequency { get; set; }

        public int NumberOfInstallments { get; set; }

        public decimal TotalInterest { get; set; }

        public decimal ProcessingFee { get; set; }

        public decimal TotalRepayable { get; set; }

        // Balance updated through repayment and adjustment services
        public decimal OutstandingBalance { get; set; }

        public string CurrencyCode { get; set; } = "KES";

        public LoanStatus Status { get; set; }
            = LoanStatus.PendingDisbursement;

        public DateOnly? DisbursementDate { get; set; }

        public DateOnly? StartDate { get; set; }

        public DateOnly? MaturityDate { get; set; }

        // Relationships
        public ICollection<LoanDisbursement> Disbursements { get; set; }
            = new List<LoanDisbursement>();

        public ICollection<RepaymentSchedule> RepaymentSchedules { get; set; }
            = new List<RepaymentSchedule>();

        public ICollection<Repayment> Repayments { get; set; }
            = new List<Repayment>();
    }
}

