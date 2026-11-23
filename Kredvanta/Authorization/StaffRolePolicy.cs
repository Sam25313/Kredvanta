namespace Kredvanta.Authorization;

public static class StaffRolePolicy
{
    public static string? Validate(IReadOnlyCollection<string> roles)
    {
        if (roles.Count == 0)
            return "Choose at least one role.";

        if (roles.Contains(AppRoles.Borrower))
            return "A staff account cannot also be a borrower.";

        if (roles.Contains(AppRoles.Administrator) && roles.Count > 1)
            return "An administrator cannot also hold case roles. Use a separate account.";

        if (roles.Contains(AppRoles.Auditor) && roles.Count > 1)
            return "An auditor cannot also hold other roles, because they would audit their own work.";

        if (roles.Contains(AppRoles.CreditAnalyst) && roles.Contains(AppRoles.CreditManager))
            return "The same person cannot be both credit analyst and credit manager.";

        return null;
    }
}