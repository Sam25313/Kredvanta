using Kredvanta.Models.Applications;
using Kredvanta.Models.Common;
using static Kredvanta.Models.Enums.LendingEnums;

namespace Kredvanta.Models.Contracts;

public class LoanAgreement : BaseEntity
{
    public Guid LoanApplicationId { get; set; }
    public LoanApplication LoanApplication { get; set; } = null!;
    public Guid LoanOfferId { get; set; }
    public LoanOffer LoanOffer { get; set; } = null!;

    public string AgreementNumber { get; set; } = string.Empty;
    public string TemplateVersion { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public string Sha256Hash { get; set; } = string.Empty;
    public AgreementStatus Status { get; set; } = AgreementStatus.Generated;
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}