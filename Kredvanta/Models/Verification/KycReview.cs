using Kredvanta.Models.Borrowers;
using Kredvanta.Models.Common;
using Kredvanta.Models.Identity;
using static Kredvanta.Models.Enums.LendingEnums;

namespace Kredvanta.Models.Verification;

public class KycReview : BaseEntity
{
    public Guid BorrowerId { get; set; }
    public Borrower Borrower { get; set; } = null!;
    public Guid? LoanApplicationId { get; set; }

    public KycStatus PreviousStatus { get; set; }
    public KycStatus Outcome { get; set; }

    public bool IdentityMatchesProfile { get; set; }
    public bool DocumentsAppearGenuine { get; set; }
    public string? ReasonCode { get; set; }
    public string? Notes { get; set; }

    public string ReviewedByUserId { get; set; } = string.Empty;
    public ApplicationUser? ReviewedByUser { get; set; }
    public DateTime ReviewedAt { get; set; } = DateTime.UtcNow;
}