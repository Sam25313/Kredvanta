using Kredvanta.Data;
using Kredvanta.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace Kredvanta.Services.Eligibility;

public class EligibilityService : IEligibilityService
{
    private readonly ApplicationDbContext _db;
    public EligibilityService(ApplicationDbContext db) => _db = db;

    public async Task<EligibilityResult> CheckAsync(Guid borrowerId, CancellationToken ct = default)
    {
        var b = await _db.Borrowers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == borrowerId, ct);
        if (b is null) return EligibilityResult.Fail(["Applicant profile not found."]);

        var reasons = new List<string>();

        if (!b.IsProfileComplete)
            reasons.Add("Complete your applicant profile first.");

        // Product rule from the design: salaried employees only.
        if (b.EmploymentStatus != EmploymentStatus.FormallyEmployed)
        {
            reasons.Add("This product is only available to formally employed applicants.");
        }
        else
        {
            if (b.EmploymentContractType is null) reasons.Add("Select your employment contract type.");
            if (b.EmploymentStartDate is null) reasons.Add("Enter your employment start date.");
            if ((b.NetMonthlySalary ?? 0) <= 0) reasons.Add("Enter your net monthly salary.");

            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            if (b.EmploymentContractType == EmploymentContractType.FixedTerm)
            {
                if (b.EmploymentEndDate is null)
                    reasons.Add("Enter your contract end date.");
                else if (b.EmploymentEndDate <= today)
                    reasons.Add("Your contract has expired.");
            }
            else if (b.EmploymentEndDate is { } end && end <= today)
            {
                reasons.Add("Your contract end date has passed.");
            }

            if (b.EmploymentContractType == EmploymentContractType.Casual)
                reasons.Add("Casual contracts are not eligible for this product.");

            // Probation and minimum tenure are credit-policy decisions that are not
            // approved yet. Do not hardcode them here.
        }

        return reasons.Count == 0 ? EligibilityResult.Ok() : EligibilityResult.Fail(reasons);
    }
}