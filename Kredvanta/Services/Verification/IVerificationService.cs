using Kredvanta.Models.Enums;
using static Kredvanta.Models.Enums.LendingEnums;

namespace Kredvanta.Services.Verification;

public record VerificationResult(bool Succeeded, string? Error = null)
{
    public static VerificationResult Ok() => new(true);
    public static VerificationResult Fail(string e) => new(false, e);
}

public class EmploymentVerificationInput
{
    public VerificationChannel Channel { get; set; }
    public string ContactedParty { get; set; } = "";
    public string? ContactPersonName { get; set; }
    public string ContactDetailUsed { get; set; } = "";
    public bool ContactIndependentlySourced { get; set; }
    public EmploymentVerificationOutcome Outcome { get; set; }
    public string? ConfirmedJobTitle { get; set; }
    public EmploymentContractType? ConfirmedContractType { get; set; }
    public DateOnly? ConfirmedStartDate { get; set; }
    public decimal? ConfirmedNetSalary { get; set; }
    public string? Notes { get; set; }
}

public interface IVerificationService
{
    Task<VerificationResult> RecordKycAsync(Guid applicationId, KycStatus outcome,
        bool identityMatchesProfile, bool documentsAppearGenuine,
        string? reasonCode, string? notes, string reviewerId, CancellationToken ct = default);

    Task<VerificationResult> RecordEmploymentAsync(Guid applicationId,
        EmploymentVerificationInput input, string verifierId, CancellationToken ct = default);
}