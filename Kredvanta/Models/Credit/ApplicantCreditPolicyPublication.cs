using Kredvanta.Models.Common;

namespace Kredvanta.Models.Credit;

public class ApplicantCreditPolicyPublication : BaseEntity
{
    public int VersionNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public bool IsPublished { get; set; }
    public DateOnly? EffectiveFrom { get; set; }
    public DateTime? PublishedAt { get; set; }
    public string? PublishedByUserId { get; set; }

    public string PurposeAndScope { get; set; } = string.Empty;
    public string EligibilityAndDocuments { get; set; } = string.Empty;
    public string AssessmentAndDecision { get; set; } = string.Empty;
    public string PricingAndOffers { get; set; } = string.Empty;
    public string RepaymentAndComplaints { get; set; } = string.Empty;
    public string PersonalInformation { get; set; } = string.Empty;
    public string ImportantNotice { get; set; } = string.Empty;

    public ICollection<ApplicantCreditPolicyReference> References { get; set; } = new List<ApplicantCreditPolicyReference>();
}

public class ApplicantCreditPolicyReference : BaseEntity
{
    public Guid ApplicantCreditPolicyPublicationId { get; set; }
    public ApplicantCreditPolicyPublication Publication { get; set; } = null!;

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
}
