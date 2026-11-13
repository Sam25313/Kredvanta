
using Kredvanta.Models.Common;
using Kredvanta.Models.Enums;
using System.ComponentModel.DataAnnotations.Schema;
using static Kredvanta.Models.Enums.LendingEnums;
using Kredvanta.Models.Loans;

namespace Kredvanta.Models.Repayments

{
    public class RepaymentSchedule : BaseEntity
    {
        public Guid LoanId { get; set; }

        public Loan Loan { get; set; } = null!;

        public int InstallmentNumber { get; set; }

        public DateOnly DueDate { get; set; }

        public decimal PrincipalDue { get; set; }

        public decimal InterestDue { get; set; }

        public decimal FeesDue { get; set; }

        public decimal TotalDue { get; set; }

        // Updated through repayment allocation transactions
        public decimal AmountPaid { get; set; }

        public ScheduleStatus Status { get; set; }
            = ScheduleStatus.Pending;

        [NotMapped]
        public decimal OutstandingAmount =>
            Math.Max(0, TotalDue - AmountPaid);

        public ICollection<RepaymentAllocation> Allocations { get; set; }
            = new List<RepaymentAllocation>();
    }
}

