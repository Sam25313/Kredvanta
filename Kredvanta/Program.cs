
using Kredvanta.Data;
using Kredvanta.Data.Seed;
using Kredvanta.Models.Identity;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// 1. DATABASE CONFIGURATION
// ==========================================

var connectionString = builder.Configuration
    .GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();


// ==========================================
// 2. ASP.NET CORE IDENTITY CONFIGURATION
// ==========================================

builder.Services
    .AddDefaultIdentity<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;

        // Password settings
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireNonAlphanumeric = false;

        // Account lockout settings
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan =
            TimeSpan.FromMinutes(15);

        // User settings
        options.User.RequireUniqueEmail = true;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();


// ==========================================
// 3. GOOGLE AUTHENTICATION
// ==========================================

var googleClientId = builder.Configuration[
    "Authentication:Google:ClientId"];

var googleClientSecret = builder.Configuration[
    "Authentication:Google:ClientSecret"];

if (!string.IsNullOrWhiteSpace(googleClientId) &&
    !string.IsNullOrWhiteSpace(googleClientSecret))
{
    builder.Services
        .AddAuthentication()
        .AddGoogle(options =>
        {
            options.ClientId = googleClientId;
            options.ClientSecret = googleClientSecret;
        });
}


// ==========================================
// 4. AUTHENTICATION COOKIE CONFIGURATION
// ==========================================

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";

    options.AccessDeniedPath =
        "/Identity/Account/AccessDenied";

    options.Cookie.Name = "Kredvanta.Auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;

    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
});


// ==========================================
// 5. MVC AND RAZOR PAGES
// ==========================================

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();


// ==========================================
// 6. BUILD APPLICATION
// ==========================================

var app = builder.Build();

// ==========================================
// 7. SEED APPLICATION ROLES AND LOAN PRODUCTS
// ==========================================

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    // --------------------------------------
    // A. SEED APPLICATION ROLES
    // --------------------------------------

    var roleManager = services
        .GetRequiredService<RoleManager<IdentityRole>>();

    string[] roles =
    {
        "Administrator",
        "CreditManager",
        "LoanOfficer",
        "Borrower"
    };

    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            var result = await roleManager.CreateAsync(
                new IdentityRole(role));

            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to create role '{role}': " +
                    string.Join(", ",
                        result.Errors.Select(e => e.Description)));
            }
        }
    }

    // --------------------------------------
    // B. ASSIGN ADMINISTRATOR ACCOUNT
    // --------------------------------------

    await IdentitySeeder.AssignAdministratorAsync(
        app.Services,
        "samuelbarasa2019@gmail.com");

    // --------------------------------------
    // SEED LOAN PRODUCTS
    // --------------------------------------

    var context = services
        .GetRequiredService<ApplicationDbContext>();

    await LoanProductSeeder.SeedAsync(context);
}
// ==========================================
// 9. HTTP REQUEST PIPELINE
// ==========================================

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();


// ==========================================
// 10. ROUTING CONFIGURATION
// ==========================================

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages()
    .WithStaticAssets();


// ==========================================
// 11. RUN APPLICATION
// ==========================================

app.Run();
