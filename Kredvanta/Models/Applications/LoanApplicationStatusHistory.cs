
using Kredvanta.Models.Common;
using Kredvanta.Models.Enums;
using Kredvanta.Models.Identity;
using static Kredvanta.Models.Enums.LendingEnums;

namespace Kredvanta.Models.Applications
{
    public class LoanApplicationStatusHistory : BaseEntity
    {
        public Guid LoanApplicationId { get; set; }

        public LoanApplication LoanApplication { get; set; } = null!;

        public LoanApplicationStatus? PreviousStatus { get; set; }

        public LoanApplicationStatus NewStatus { get; set; }

        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

        public string? ChangedByUserId { get; set; }

        public ApplicationUser? ChangedByUser { get; set; }

        public string? Notes { get; set; }
    }
}
