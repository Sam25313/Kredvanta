using System.Security.Cryptography;
using Kredvanta.Authorization;
using Kredvanta.Data;
using Kredvanta.Models.Identity;
using Kredvanta.Services.Audit;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Kredvanta.Services.Staff;

public record StaffResult(bool Succeeded, string? Error = null, string? TempPassword = null)
{
    public static StaffResult Fail(string e) => new(false, e);
    public static StaffResult Ok(string? temp = null) => new(true, null, temp);
}

public record StaffListItem(string Id, string Name, string Email, IReadOnlyList<string> Roles, bool IsActive)
{
    public bool IsAdministrator => Roles.Contains(AppRoles.Administrator);
}

public interface IStaffManagementService
{
    Task<List<StaffListItem>> ListAsync(CancellationToken ct = default);
    Task<StaffListItem?> GetAsync(string userId, CancellationToken ct = default);
    Task<StaffResult> CreateAsync(string email, string firstName, string lastName,
        IEnumerable<string> presetKeys, string adminId);
    Task<StaffResult> SetPresetsAsync(string userId, IEnumerable<string> presetKeys, string adminId);
    Task<StaffResult> SetActiveAsync(string userId, bool active, string adminId);
}

public class StaffManagementService : IStaffManagementService
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _users;
    private readonly IAuditService _audit;

    public StaffManagementService(ApplicationDbContext db, UserManager<ApplicationUser> users, IAuditService audit)
    {
        _db = db; _users = users; _audit = audit;
    }

    public async Task<List<StaffListItem>> ListAsync(CancellationToken ct = default)
    {
        var rows = await (from ur in _db.UserRoles
                          join r in _db.Roles on ur.RoleId equals r.Id
                          join u in _db.Users on ur.UserId equals u.Id
                          where r.Name != AppRoles.Borrower
                          select new { u.Id, u.Email, u.FirstName, u.LastName, u.IsActive, Role = r.Name })
                         .AsNoTracking().ToListAsync(ct);

        return rows.GroupBy(x => new { x.Id, x.Email, x.FirstName, x.LastName, x.IsActive })
            .Select(g => new StaffListItem(g.Key.Id, $"{g.Key.FirstName} {g.Key.LastName}".Trim(),
                g.Key.Email ?? "", g.Select(x => x.Role!).OrderBy(x => x).ToList(), g.Key.IsActive))
            .OrderBy(x => x.Name).ToList();
    }

    public async Task<StaffListItem?> GetAsync(string userId, CancellationToken ct = default)
        => (await ListAsync(ct)).FirstOrDefault(x => x.Id == userId);

    public async Task<StaffResult> CreateAsync(string email, string firstName, string lastName,
        IEnumerable<string> presetKeys, string adminId)
    {
        var roles = StaffPresets.RolesFor(presetKeys);
        var error = StaffRolePolicy.Validate(roles);
        if (error is not null) return StaffResult.Fail(error);

        email = email.Trim();
        if (await _users.FindByEmailAsync(email) is not null)
            return StaffResult.Fail("A user with this email already exists.");

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            IsActive = true,
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            LockoutEnabled = true
        };

        var temp = PasswordGenerator.Create();
        var created = await _users.CreateAsync(user, temp);
        if (!created.Succeeded) return StaffResult.Fail(Join(created));

        var added = await _users.AddToRolesAsync(user, roles);
        if (!added.Succeeded)
        {
            await _users.DeleteAsync(user);   // brand new, so no history to protect
            return StaffResult.Fail(Join(added));
        }

        await _audit.LogAsync("StaffCreated", "ApplicationUser", user.Id, null,
            new { user.Email, Roles = roles });   // never log the password

        return StaffResult.Ok(temp);
    }

    public async Task<StaffResult> SetPresetsAsync(string userId, IEnumerable<string> presetKeys, string adminId)
    {
        if (userId == adminId) return StaffResult.Fail("You cannot change your own roles.");

        var user = await _users.FindByIdAsync(userId);
        if (user is null) return StaffResult.Fail("User not found.");

        var current = (await _users.GetRolesAsync(user)).ToList();
        if (current.Contains(AppRoles.Administrator))
            return StaffResult.Fail("Administrator accounts are not managed here.");

        var target = StaffPresets.RolesFor(presetKeys);
        var error = StaffRolePolicy.Validate(target);
        if (error is not null) return StaffResult.Fail(error);

        var toRemove = current.Where(r => AppRoles.CaseReaders.Contains(r) && !target.Contains(r)).ToList();
        var toAdd = target.Where(r => !current.Contains(r)).ToList();

        if (toAdd.Count > 0)
        {
            var r = await _users.AddToRolesAsync(user, toAdd);
            if (!r.Succeeded) return StaffResult.Fail(Join(r));
        }
        if (toRemove.Count > 0)
        {
            var r = await _users.RemoveFromRolesAsync(user, toRemove);
            if (!r.Succeeded) return StaffResult.Fail(Join(r));
        }

        await _users.UpdateSecurityStampAsync(user);   // takes effect on their next request

        await _audit.LogAsync("StaffRolesChanged", "ApplicationUser", user.Id, null,
            new { Before = current, After = target });

        return StaffResult.Ok();
    }

    public async Task<StaffResult> SetActiveAsync(string userId, bool active, string adminId)
    {
        if (userId == adminId) return StaffResult.Fail("You cannot deactivate your own account.");

        var user = await _users.FindByIdAsync(userId);
        if (user is null) return StaffResult.Fail("User not found.");

        if (!active && await _users.IsInRoleAsync(user, AppRoles.Administrator))
        {
            var admins = await _users.GetUsersInRoleAsync(AppRoles.Administrator);
            if (admins.Count(a => a.IsActive && a.Id != user.Id) == 0)
                return StaffResult.Fail("This is the last active administrator and cannot be deactivated.");
        }

        user.IsActive = active;
        user.LockoutEnabled = true;
        // Deactivation locks sign-in; reactivation clears the lock.
        await _users.SetLockoutEndDateAsync(user, active ? null : DateTimeOffset.MaxValue);
        var result = await _users.UpdateAsync(user);
        if (!result.Succeeded) return StaffResult.Fail(Join(result));

        await _users.UpdateSecurityStampAsync(user);   // ends any signed-in session

        await _audit.LogAsync(active ? "StaffReactivated" : "StaffDeactivated", "ApplicationUser", user.Id);
        return StaffResult.Ok();
    }

    private static string Join(IdentityResult r) => string.Join(" ", r.Errors.Select(e => e.Description));
}

internal static class PasswordGenerator
{
    public static string Create()
    {
        const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string lower = "abcdefghijkmnopqrstuvwxyz";
        const string digits = "23456789";
        var all = upper + lower + digits;

        var chars = new List<char> { Pick(upper), Pick(lower), Pick(digits) };
        for (var i = 0; i < 9; i++) chars.Add(Pick(all));

        // Fisher-Yates shuffle
        for (var i = chars.Count - 1; i > 0; i--)
        {
            var j = RandomNumberGenerator.GetInt32(i + 1);
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }
        return new string(chars.ToArray());
    }

    private static char Pick(string s) => s[RandomNumberGenerator.GetInt32(s.Length)];
}