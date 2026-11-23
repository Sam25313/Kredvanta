using Kredvanta.Models.Identity;
using Microsoft.AspNetCore.Identity;

namespace Kredvanta.Services.Workflow;

public interface IUserRoleReader
{
    /// Returns null when the user does not exist.
    Task<IReadOnlyList<string>?> GetRolesAsync(string userId, CancellationToken ct = default);
}

public class UserRoleReader : IUserRoleReader
{
    private readonly UserManager<ApplicationUser> _users;
    public UserRoleReader(UserManager<ApplicationUser> users) => _users = users;

    public async Task<IReadOnlyList<string>?> GetRolesAsync(string userId, CancellationToken ct = default)
    {
        var user = await _users.FindByIdAsync(userId);
        if (user is null) return null;
        return (await _users.GetRolesAsync(user)).ToList();
    }
}