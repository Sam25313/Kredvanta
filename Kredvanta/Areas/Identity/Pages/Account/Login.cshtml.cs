
using Kredvanta.Authorization;
using Kredvanta.Models.Identity;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using System.ComponentModel.DataAnnotations;

namespace Kredvanta.Areas.Identity.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<LoginModel> _logger;

        public LoginModel(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            ILogger<LoginModel> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public IList<AuthenticationScheme> ExternalLogins { get; set; }
            = new List<AuthenticationScheme>();

        public string? ReturnUrl { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; } = string.Empty;

            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; } = string.Empty;

            [Display(Name = "Remember me?")]
            public bool RememberMe { get; set; }
        }

        // ==========================================
        // GET: LOGIN PAGE
        // ==========================================

        public async Task<IActionResult> OnGetAsync(
            string? returnUrl = null)
        {
            returnUrl ??= Url.Content("~/go");
            //returnUrl ??= Url.Content("~/");

            // Redirect already authenticated users
            // according to their assigned roles.
            if (User.Identity?.IsAuthenticated == true)
            {
                var currentUser =
                    await _userManager.GetUserAsync(User);

                if (currentUser != null)
                {
                    return await RedirectToRoleDashboardAsync(
                        currentUser);
                }

                return Challenge();
            }

            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(
                    string.Empty,
                    ErrorMessage);
            }

            await HttpContext.SignOutAsync(
                IdentityConstants.ExternalScheme);

            await LoadExternalLoginsAsync();

            ReturnUrl = returnUrl;

            return Page();
        }

        // ==========================================
        // POST: AUTHENTICATE USER
        // ==========================================

        public async Task<IActionResult> OnPostAsync(
            string? returnUrl = null)
        {
            returnUrl ??= Url.Content("~/go");
            //returnUrl ??= Url.Content("~/");
            ReturnUrl = returnUrl;

            await LoadExternalLoginsAsync();

            if (!ModelState.IsValid)
                return Page();

            var email = Input.Email.Trim();

            // 1. Find existing account.
            var user = await _userManager.FindByEmailAsync(email);

            if (user == null || !user.IsActive)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Invalid login attempt or account is inactive.");

                return Page();
            }

            // 2. Authenticate user.
            var result = await _signInManager.PasswordSignInAsync(
                user,
                Input.Password,
                Input.RememberMe,
                lockoutOnFailure: true);

            if (result.Succeeded)
            {
                _logger.LogInformation(
                    "User {UserId} logged in successfully.",
                    user.Id);

                // 3. Redirect according to assigned role.
                return await RedirectToRoleDashboardAsync(user);
            }

            // 4. Two-factor authentication.
            if (result.RequiresTwoFactor)
            {
                return RedirectToPage(
                    "./LoginWith2fa",
                    new
                    {
                        ReturnUrl = returnUrl,
                        RememberMe = Input.RememberMe
                    });
            }

            // 5. Account locked out.
            if (result.IsLockedOut)
            {
                _logger.LogWarning(
                    "User {UserId} account is locked out.",
                    user.Id);

                return RedirectToPage("./Lockout");
            }

            // 6. Invalid credentials.
            ModelState.AddModelError(
                string.Empty,
                "Invalid login attempt.");

            return Page();
        }

        // ==========================================
        // ROLE-BASED DASHBOARD REDIRECTION
        // ==========================================

        private async Task<IActionResult> RedirectToRoleDashboardAsync(
            ApplicationUser user)
        {
            var roles = await _userManager.GetRolesAsync(user);

            // Administrator has the highest priority.
            if (roles.Contains(AppRoles.Administrator))
            {
                return RedirectToAction(
                    "Index", "AdminDashboard");
            }

            if (roles.Any(AppRoles.CaseReaders.Contains))
            {
                return RedirectToAction(
                    "Index", "StaffHome");
            }

            if (roles.Contains(AppRoles.Borrower))
            {
                return RedirectToAction(
                    "Index", "Dashboard");
            }

            // Authenticated account without an assigned role.
            _logger.LogWarning(
                "User {UserId} has no recognized application role.",
                user.Id);

            return Forbid();
        }

        // ==========================================
        // GOOGLE EXTERNAL LOGIN
        // ==========================================

        private async Task LoadExternalLoginsAsync()
        {
            var schemes =
                await _signInManager.GetExternalAuthenticationSchemesAsync();

            ExternalLogins = schemes
                .Where(s => s.Name == "Google")
                .ToList();
        }
    }
}
