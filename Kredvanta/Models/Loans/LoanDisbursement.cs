
using Kredvanta.Models.Common;
using Kredvanta.Models.Enums;
using Kredvanta.Models.Identity;
using static Kredvanta.Models.Enums.LendingEnums;

namespace Kredvanta.Models.Loans
{
    public class LoanDisbursement : BaseEntity
    {
        public Guid LoanId { get; set; }

        public Loan Loan { get; set; } = null!;

        public decimal Amount { get; set; }

        public DisbursementMethod Method { get; set; }

        public DisbursementStatus Status { get; set; }
            = DisbursementStatus.Pending;

        public string? TransactionReference { get; set; }

        public string? RecipientAccount { get; set; }

        public string ProcessedByUserId { get; set; } = string.Empty;

        public ApplicationUser? ProcessedByUser { get; set; }

        public DateTime? DisbursedAt { get; set; }

        public string? Notes { get; set; }
    }
}

