
using Kredvanta.Models.Common;
using Kredvanta.Models.Borrowers;

namespace Kredvanta.Models.Applications
{
    public class LoanApplicationGuarantor : BaseEntity
    {
        public Guid LoanApplicationId { get; set; }

        public LoanApplication LoanApplication { get; set; } = null!;

        public Guid GuarantorId { get; set; }

        public Guarantor Guarantor { get; set; } = null!;

        public decimal? GuaranteedAmount { get; set; }

        public bool HasAcceptedGuarantee { get; set; }
    }
}

