using Kredvanta.Models.Common;
using Kredvanta.Models.Identity;

namespace Kredvanta.Models.Contracts;

public class AgreementSignature : BaseEntity
{
    public Guid LoanAgreementId { get; set; }
    public LoanAgreement LoanAgreement { get; set; } = null!;

    public string SignedByUserId { get; set; } = string.Empty;
    public ApplicationUser? SignedByUser { get; set; }
    public DateTime SignedAt { get; set; } = DateTime.UtcNow;

    public string Method { get; set; } = string.Empty;       // e.g. PasswordReauthentication
    public string AgreementHash { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}