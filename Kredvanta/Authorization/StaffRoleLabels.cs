using System.Security.Claims;

namespace Kredvanta.Authorization;

public static class StaffRoleLabels
{
    public static string Friendly(string role) => role switch
    {
        AppRoles.Administrator => "Administrator",
        AppRoles.LoanOfficer => "Loan officer",
        AppRoles.KycOfficer => "KYC officer",
        AppRoles.VerificationOfficer => "Verification officer",
        AppRoles.CreditAnalyst => "Credit analyst",
        AppRoles.CreditManager => "Credit manager",
        AppRoles.FinanceOfficer => "Finance officer",
        AppRoles.Auditor => "Auditor",
        _ => role
    };

    /// "Operations officer + Credit analyst"
    public static string Describe(IEnumerable<string> roles)
    {
        var left = roles.Where(r => r != AppRoles.Borrower).ToHashSet();
        var labels = new List<string>();

        foreach (var p in StaffPresets.All)
        {
            if (!p.Roles.All(left.Contains)) continue;
            labels.Add(p.Label);
            foreach (var r in p.Roles) left.Remove(r);
        }

        labels.AddRange(left.OrderBy(r => r).Select(Friendly));
        return string.Join(" + ", labels);
    }

    public static string Describe(ClaimsPrincipal user)
        => Describe(AppRoles.All.Where(user.IsInRole));
}