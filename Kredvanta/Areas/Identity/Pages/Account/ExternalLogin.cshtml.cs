
using Kredvanta.Models.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kredvanta.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class ExternalLoginModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<ExternalLoginModel> _logger;

        public ExternalLoginModel(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            ILogger<ExternalLoginModel> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
        }

        [TempData]
        public string? ErrorMessage { get; set; }
        public string? ProviderDisplayName { get; set; }

        public IActionResult OnGet()
        {
            return RedirectToPage("./Login");
        }

        // Start Google authentication.
        public IActionResult OnPost(
            string provider,
            string? returnUrl = null)
        {
            // Kredvanta currently supports Google only.
            if (!string.Equals(
                provider,
                "Google",
                StringComparison.OrdinalIgnoreCase))
            {
                ErrorMessage = "This sign-in provider is not supported.";
                return RedirectToPage("./Login");
            }

            returnUrl ??= Url.Content("~/");

            var redirectUrl = Url.Page(
                "./ExternalLogin",
                pageHandler: "Callback",
                values: new { returnUrl });

            var properties =
                _signInManager.ConfigureExternalAuthenticationProperties(
                    provider,
                    redirectUrl!);

            return Challenge(properties, provider);
        }

        // Google redirects the user here after authentication.
        public async Task<IActionResult> OnGetCallbackAsync(
            string? returnUrl = null,
            string? remoteError = null)
        {
            returnUrl ??= Url.Content("~/");

            if (!string.IsNullOrEmpty(remoteError))
            {
                ErrorMessage =
                    "Google sign-in was unsuccessful. Please try again.";

                return RedirectToPage(
                    "./Login",
                    new { ReturnUrl = returnUrl });
            }

            var externalInfo =
                await _signInManager.GetExternalLoginInfoAsync();

            if (externalInfo == null)
            {
                ErrorMessage =
                    "Unable to retrieve your Google account information. Please try again.";

                return RedirectToPage(
                    "./Login",
                    new { ReturnUrl = returnUrl });
            }

            // Only accept Google authentication.
            if (!string.Equals(
                externalInfo.LoginProvider,
                "Google",
                StringComparison.OrdinalIgnoreCase))
            {
                await HttpContext.SignOutAsync(
                    IdentityConstants.ExternalScheme);

                ErrorMessage = "Unsupported sign-in provider.";

                return RedirectToPage("./Login");
            }

            // Find an existing Kredvanta account linked to this Google login.
            var user = await _userManager.FindByLoginAsync(
                externalInfo.LoginProvider,
                externalInfo.ProviderKey);

            // Do not create an account if this Google identity
            // has never been linked to Kredvanta.
            if (user == null)
            {
                await HttpContext.SignOutAsync(
                    IdentityConstants.ExternalScheme);

                ErrorMessage =
                    "This Google account is not linked to an existing Kredvanta account. Sign in using your registered email and password first.";

                return RedirectToPage("./Login");
            }

            // Prevent inactive accounts from signing in.
            if (!user.IsActive)
            {
                await HttpContext.SignOutAsync(
                    IdentityConstants.ExternalScheme);

                ErrorMessage =
                    "Your Kredvanta account is currently inactive. Please contact support.";

                _logger.LogWarning(
                    "Inactive user {UserId} attempted Google sign-in.",
                    user.Id);

                return RedirectToPage("./Login");
            }

            // Authenticate the existing linked account.
            var result = await _signInManager.ExternalLoginSignInAsync(
                externalInfo.LoginProvider,
                externalInfo.ProviderKey,
                isPersistent: false,
                bypassTwoFactor: true);

            await HttpContext.SignOutAsync(
                IdentityConstants.ExternalScheme);

            if (result.Succeeded)
            {
                _logger.LogInformation(
                    "Existing Kredvanta user {UserId} signed in using Google.",
                    user.Id);

                return LocalRedirect(returnUrl);
            }

            if (result.IsLockedOut)
            {
                _logger.LogWarning(
                    "Google sign-in attempted on locked account {UserId}.",
                    user.Id);

                return RedirectToPage("./Lockout");
            }

            ErrorMessage =
                "Google sign-in could not be completed. Please try your email and password.";

            return RedirectToPage(
                "./Login",
                new { ReturnUrl = returnUrl });
        }
    }
}
