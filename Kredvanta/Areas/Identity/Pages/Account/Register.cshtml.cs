
using System.ComponentModel.DataAnnotations;
using Kredvanta.Models.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kredvanta.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<RegisterModel> _logger;

        public RegisterModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ILogger<RegisterModel> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [Required]
            [StringLength(50)]
            [Display(Name = "First name")]
            public string FirstName { get; set; } = string.Empty;

            [Required]
            [StringLength(50)]
            [Display(Name = "Last name")]
            public string LastName { get; set; } = string.Empty;

            [Required]
            [Phone]
            [Display(Name = "Phone number")]
            public string PhoneNumber { get; set; } = string.Empty;

            [Required]
            [EmailAddress]
            public string Email { get; set; } = string.Empty;

            [Required]
            [StringLength(100, MinimumLength = 8)]
            [DataType(DataType.Password)]
            public string Password { get; set; } = string.Empty;

            [Required]
            [DataType(DataType.Password)]
            [Compare(nameof(Password),
                ErrorMessage = "Passwords do not match.")]
            [Display(Name = "Confirm password")]
            public string ConfirmPassword { get; set; } = string.Empty;

            [Range(typeof(bool), "true", "true",
                ErrorMessage = "You must accept the terms and conditions.")]
            [Display(Name = "Accept terms")]
            public bool AcceptTerms { get; set; }
        }

        // ==========================================
        // REGISTER USING EMAIL AND PASSWORD
        // ==========================================

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            var email = Input.Email.Trim();

            // Check whether this email is already registered.
            var existingUser =
                await _userManager.FindByEmailAsync(email);

            if (existingUser != null)
            {
                ModelState.AddModelError(
                    "Input.Email",
                    "This email address is already registered.");

                return Page();
            }

            // Create the borrower account.
            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                PhoneNumber = Input.PhoneNumber.Trim(),
                FirstName = Input.FirstName.Trim(),
                LastName = Input.LastName.Trim(),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var createResult = await _userManager.CreateAsync(
                user,
                Input.Password);

            if (!createResult.Succeeded)
            {
                foreach (var error in createResult.Errors)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        error.Description);
                }

                return Page();
            }

            // Assign the Borrower role.
            var roleResult = await _userManager.AddToRoleAsync(
                user,
                "Borrower");

            if (!roleResult.Succeeded)
            {
                _logger.LogError(
                    "Failed to assign Borrower role to user {UserId}",
                    user.Id);

                await _userManager.DeleteAsync(user);

                ModelState.AddModelError(
                    string.Empty,
                    "Registration could not be completed. Please try again.");

                return Page();
            }

            _logger.LogInformation(
                "New borrower registered: {UserId}",
                user.Id);

            // Sign in the newly registered borrower.
            await _signInManager.SignInAsync(
                user,
                isPersistent: false);

            // Redirect to borrower profile creation.
            return Redirect("/Profile/Step1");
        }
    }
}
