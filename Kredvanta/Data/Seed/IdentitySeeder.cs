using Kredvanta.Authorization;
using Kredvanta.Models.Identity;
using Microsoft.AspNetCore.Identity;

namespace Kredvanta.Data.Seed;

public static class IdentitySeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider, IConfiguration configuration)
    {
        using var scope = serviceProvider.CreateScope();
        var services = scope.ServiceProvider;

        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("IdentitySeeder");

        await EnsureRolesAsync(roleManager);
        await EnsureAdministratorAsync(userManager, configuration, logger);

        logger.LogInformation("Identity seeding complete.");
    }

    private static async Task EnsureRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        foreach (var role in AppRoles.All)
        {
            if (await roleManager.RoleExistsAsync(role)) continue;
            ThrowIfFailed(await roleManager.CreateAsync(new IdentityRole(role)),
                $"Failed to create role '{role}'");
        }
    }

    private static async Task EnsureAdministratorAsync(
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration,
        ILogger logger)
    {
        var email = configuration["SeedAdmin:Email"];
        if (string.IsNullOrWhiteSpace(email))
        {
            logger.LogWarning(
                "Administrator seeding was skipped because SeedAdmin:Email is not configured.");
            return;
        }

        var admin = await userManager.FindByEmailAsync(email);

        if (admin is null)
        {
            var password = configuration["SeedAdmin:Password"];
            if (string.IsNullOrWhiteSpace(password))
            {
                logger.LogWarning(
                    "Administrator seeding was skipped because SeedAdmin:Password is not configured and the administrator account does not exist.");
                return;
            }

            admin = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                IsActive = true
            };

            ThrowIfFailed(await userManager.CreateAsync(admin, password),
                "Failed to create the initial administrator");
        }

        if (!await userManager.IsInRoleAsync(admin, AppRoles.Administrator))
        {
            ThrowIfFailed(await userManager.AddToRoleAsync(admin, AppRoles.Administrator),
                "Failed to assign Administrator role");
        }
    }

    private static void ThrowIfFailed(IdentityResult result, string message)
    {
        if (result.Succeeded) return;
        throw new InvalidOperationException(
            $"{message}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
    }
}