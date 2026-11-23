using System.Threading.RateLimiting;
using Kredvanta.Authorization;
using Kredvanta.Data;
using Kredvanta.Data.Seed;
using Kredvanta.Models.Identity;
using Kredvanta.Services.Audit;
using Kredvanta.Services.Credit;
using Kredvanta.Services.Documents;
using Kredvanta.Services.Eligibility;
using Kredvanta.Services.Journey;
using Kredvanta.Services.Locations;
using Kredvanta.Services.Storage;
using Kredvanta.Services.Verification;
using Kredvanta.Services.Workflow;
using Kredvanta.Services.Staff;
using Kredvanta.Services.Contracts;
using Kredvanta.Services.Disbursement;
using Kredvanta.Services.Repayments;
using Kredvanta.Services.Documents.Screening;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;


var builder = WebApplication.CreateBuilder(args);

// 1. DATABASE
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// 2. IDENTITY
builder.Services
    .AddDefaultIdentity<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireNonAlphanumeric = false;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.User.RequireUniqueEmail = true;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

// 3. GOOGLE (optional)
var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];

if (!string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret))
{
    builder.Services.AddAuthentication().AddGoogle(options =>
    {
        options.ClientId = googleClientId;
        options.ClientSecret = googleClientSecret;
    });
}

// 4. AUTH COOKIE
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.AccessDeniedPath = "/Home/AccessDenied";
    options.Cookie.Name = "Kredvanta.Auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    options.SlidingExpiration = true;
});

// 5. AUTHORIZATION
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CaseReader", policy => policy.RequireRole(AppRoles.CaseReaders));
});
builder.Services.AddScoped<IAuthorizationHandler, LoanApplicationAuthorizationHandler>();
builder.Services.Configure<SecurityStampValidatorOptions>(o => o.ValidationInterval = TimeSpan.Zero);
builder.Services.AddScoped<IStaffManagementService, StaffManagementService>();

// 5b. RATE LIMITING
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // General cap: per signed-in user, or per IP when anonymous
    o.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
        RateLimitPartition.GetFixedWindowLimiter(
            ctx.User.Identity?.Name ?? ctx.Connection.RemoteIpAddress?.ToString() ?? "anon",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 120,
                Window = TimeSpan.FromMinutes(1)
            }));

    // Tighter cap for sensitive actions, applied with [EnableRateLimiting("sensitive")]
    o.AddPolicy("sensitive", ctx => RateLimitPartition.GetFixedWindowLimiter(
        ctx.User.Identity?.Name ?? ctx.Connection.RemoteIpAddress?.ToString() ?? "anon",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1)
        }));

    // Friendly message instead of a bare 429
    o.OnRejected = async (context, ct) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await context.HttpContext.Response.WriteAsync(
            "Too many requests. Please wait a minute and try again.", ct);
    };
});

// 6. APPLICATION SERVICES
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IUserRoleReader, UserRoleReader>();
builder.Services.AddScoped<IApplicationWorkflowService, ApplicationWorkflowService>();
builder.Services.AddScoped<IDisbursementService, DisbursementService>();
builder.Services.AddScoped<ITransitionGuard, DisbursementGuard>();
builder.Services.AddScoped<ITransitionGuard, RequiredDocumentsGuard>();
builder.Services.AddScoped<ITransitionGuard, ProductAvailabilityGuard>();
builder.Services.AddSingleton<IFileStorageService, LocalFileStorageService>();

builder.Services.AddScoped<IEligibilityService, EligibilityService>();
builder.Services.AddScoped<ITransitionGuard, EligibilityGuard>();
builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddSingleton<IKenyaLocationCatalog, KenyaLocationCatalog>();

builder.Services.AddScoped<IVerificationService, VerificationService>();
builder.Services.AddScoped<ITransitionGuard, EmploymentVerificationGuard>();
builder.Services.AddScoped<IBorrowerJourneyService, BorrowerJourneyService>();

builder.Services.AddScoped<IAppraisalService, AppraisalService>();
builder.Services.AddScoped<ITransitionGuard, AssessmentGuard>();
builder.Services.AddScoped<IApprovalService, ApprovalService>();
builder.Services.AddScoped<IOfferService, OfferService>();
builder.Services.AddScoped<ITransitionGuard, AgreementReadinessGuard>();
builder.Services.AddHostedService<OfferExpiryWorker>();
builder.Services.AddScoped<IRepaymentService, RepaymentService>();
builder.Services.AddHostedService<OverdueWorker>();

builder.Services.AddSingleton<IDocumentScreeningQueue, DocumentScreeningQueue>();
builder.Services.AddSingleton<IDocumentTextExtractor, DocumentTextExtractor>();
builder.Services.AddScoped<IDocumentScreeningService, DocumentScreeningService>();
builder.Services.AddScoped<ITransitionGuard, ScreeningCompleteGuard>();
builder.Services.AddHostedService<DocumentScreeningWorker>();

// 7. MVC + RAZOR PAGES (CSRF protection on all state-changing requests)
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});
builder.Services.AddRazorPages();

var app = builder.Build();

// Security headers (above static files so they get them too)
app.Use(async (ctx, next) =>
{
    var h = ctx.Response.Headers;
    h["X-Content-Type-Options"] = "nosniff";
    h["X-Frame-Options"] = "DENY";
    h["Referrer-Policy"] = "strict-origin-when-cross-origin";
    h["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
    // Allows your current CDN usage (Bootstrap Icons) and inline styles/scripts used by the views.
    h["Content-Security-Policy"] =
        "default-src 'self'; img-src 'self' data: https:; " +
        "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net; " +
        "font-src 'self' https://cdn.jsdelivr.net; " +
        "script-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net; frame-ancestors 'none'";
    await next();
});

// 8. SEED
await IdentitySeeder.SeedAsync(app.Services, builder.Configuration);

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await Kredvanta.Data.Seed.LoanProductSeeder.SeedAsync(context, app.Environment.IsDevelopment());
    await Kredvanta.Data.Seed.ApplicantCreditPolicySeeder.SeedAsync(context);
}

// 9. PIPELINE
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
app.UseRateLimiter();          // after routing, before authentication
app.UseAuthentication();
app.UseAuthorization();

// 10. ROUTING
app.MapStaticAssets();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();
app.MapRazorPages().WithStaticAssets();

app.Run();