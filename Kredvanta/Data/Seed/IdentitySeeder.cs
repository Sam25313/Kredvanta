using Kredvanta.Models.Identity;
using Microsoft.AspNetCore.Identity;

namespace Kredvanta.Data.Seed
{
    public static class IdentitySeeder
    {
        public static async Task AssignAdministratorAsync(
            IServiceProvider serviceProvider,
            string email)
        {
            using var scope = serviceProvider.CreateScope();

            var roleManager = scope.ServiceProvider
                .GetRequiredService<RoleManager<IdentityRole>>();

            var userManager = scope.ServiceProvider
                .GetRequiredService<UserManager<ApplicationUser>>();

            // 1. Ensure the Administrator role exists.
            if (!await roleManager.RoleExistsAsync("Administrator"))
            {
                var roleResult = await roleManager.CreateAsync(
                    new IdentityRole("Administrator"));

                if (!roleResult.Succeeded)
                {
                    throw new Exception(
                        "Could not create Administrator role: " +
                        string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                }
            }

            // 2. Find the user using their email address.
            var user = await userManager.FindByEmailAsync(email);

            if (user == null)
            {
                throw new Exception(
                    $"No registered user was found with email: {email}");
            }

            // 3. Assign the Administrator role.
            if (!await userManager.IsInRoleAsync(user, "Administrator"))
            {
                var result = await userManager.AddToRoleAsync(
                    user, "Administrator");

                if (!result.Succeeded)
                {
                    throw new Exception(
                        "Could not assign Administrator role: " +
                        string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
            var isAdmin = await userManager.IsInRoleAsync(
    user, "Administrator");

            Console.WriteLine(
                $"USER: {user.Email} | ADMINISTRATOR: {isAdmin}");
        }
    }
}