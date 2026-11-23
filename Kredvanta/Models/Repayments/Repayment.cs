
using Kredvanta.Models.Common;
using Kredvanta.Models.Enums;
using Kredvanta.Models.Identity;
using Kredvanta.Models.Loans;
using static Kredvanta.Models.Enums.LendingEnums;
using System.ComponentModel.DataAnnotations;

namespace Kredvanta.Models.Repayments
{
    public class Repayment : BaseEntity
    {
        public string ReceiptNumber { get; set; } = string.Empty;

        public Guid LoanId { get; set; }

        public Loan Loan { get; set; } = null!;

        public decimal Amount { get; set; }

        public RepaymentMethod Method { get; set; }

        public RepaymentStatus Status { get; set; }
            = RepaymentStatus.Pending;

        public string? TransactionReference { get; set; }

        // External provider transaction ID, e.g. M-Pesa receipt ID
        public string? ExternalTransactionId { get; set; }

        public string? PayerPhoneNumber { get; set; }

        public string ReceivedByUserId { get; set; } = string.Empty;

        public ApplicationUser? ReceivedByUser { get; set; }

        public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ConfirmedAt { get; set; }
        public string? ReversalReason { get; set; }
        public DateTime? ReversedAt { get; set; }
        public string? ReversedByUserId { get; set; }
       
       
        
        

        public string? Notes { get; set; }
        [Timestamp]
        public byte[] RowVersion { get; set; } = null!;

        public ICollection<RepaymentAllocation> Allocations { get; set; }
            = new List<RepaymentAllocation>();
    }
}
