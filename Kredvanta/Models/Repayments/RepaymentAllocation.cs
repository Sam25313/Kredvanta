
using Kredvanta.Models.Common;

namespace Kredvanta.Models.Repayments
{
    public class RepaymentAllocation : BaseEntity
    {
        public Guid RepaymentId { get; set; }

        public Repayment Repayment { get; set; } = null!;

        public Guid RepaymentScheduleId { get; set; }

        public RepaymentSchedule RepaymentSchedule { get; set; } = null!;

        public decimal PrincipalAmount { get; set; }

        public decimal InterestAmount { get; set; }

        public decimal FeesAmount { get; set; }

        public decimal AllocatedAmount { get; set; }
    }
}
