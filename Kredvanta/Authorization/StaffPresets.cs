namespace Kredvanta.Authorization;

public record StaffPreset(string Key, string Label, string Description, string[] Roles);

public static class StaffPresets
{
    public static readonly StaffPreset[] All =
    {
        new("operations", "Operations officer",
            "Receives applications, checks identity and documents, and verifies employers.",
            new[] { AppRoles.LoanOfficer, AppRoles.KycOfficer, AppRoles.VerificationOfficer }),

        new("analyst", "Credit analyst",
            "Assesses affordability and recommends applications.",
            new[] { AppRoles.CreditAnalyst }),

        new("manager", "Credit manager",
            "Makes the final decision on recommended applications.",
            new[] { AppRoles.CreditManager }),

        new("finance", "Finance",
            "Handles disbursement and repayments.",
            new[] { AppRoles.FinanceOfficer }),

        new("auditor", "Auditor",
            "Read-only access to records and the audit log. Cannot be combined with other roles.",
            new[] { AppRoles.Auditor }),
    };

    public static string[] RolesFor(IEnumerable<string>? keys)
    {
        var chosen = (keys ?? Enumerable.Empty<string>()).ToList();
        return All.Where(p => chosen.Contains(p.Key))
                  .SelectMany(p => p.Roles).Distinct().ToArray();
    }

    public static IEnumerable<string> KeysFor(IEnumerable<string> roles)
    {
        var set = roles.ToHashSet();
        return All.Where(p => p.Roles.All(set.Contains)).Select(p => p.Key);
    }
}