
using Kredvanta.Data;
using Kredvanta.Models;
using Kredvanta.Models.Borrowers;
using Kredvanta.Models.Enums;
using Kredvanta.Models.Identity;
using Kredvanta.ViewModels.Profile;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Kredvanta.Controllers
{
    [Authorize(Roles = "Borrower")]
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ProfileController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // =========================================================
        // STEP 1: PERSONAL IDENTIFICATION
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Step1()
        {
            var borrower = await GetCurrentBorrowerAsync();

            if (borrower == null)
            {
                return View("Step1", new CreateProfileViewModel
                {
                    CurrentStep = 1
                });
            }

            var model = BuildViewModel(borrower);
            model.CurrentStep = 1;

            return View("Step1", model);
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Step1(
            CreateProfileViewModel model)
        {
            return await SaveStep1Async(model);
        }

        private async Task<IActionResult> SaveStep1Async(
            CreateProfileViewModel model)
        {
            model.CurrentStep = 1;

            ValidateProperties(
                model,
                nameof(model.FirstName),
                nameof(model.MiddleName),
                nameof(model.LastName),
                nameof(model.DateOfBirth),
                nameof(model.Gender),
                nameof(model.IdentificationType),
                nameof(model.NationalIdNumber)
            );

            ValidateDateOfBirth(model);

            KeepModelStateForProperties(
                nameof(model.FirstName),
                nameof(model.MiddleName),
                nameof(model.LastName),
                nameof(model.DateOfBirth),
                nameof(model.Gender),
                nameof(model.IdentificationType),
                nameof(model.NationalIdNumber)
            );

            if (!ModelState.IsValid)
            {
                return View("Step1", model);
            }

            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }

            var borrower = await GetCurrentBorrowerAsync();

            // Convert ViewModel identification enum
            // into the Borrower entity identification enum.
            var borrowerIdentificationType =
                Enum.Parse<Kredvanta.Models.Enums.IdentificationType>(
                    model.IdentificationType.ToString(),
                    ignoreCase: true
                );

            var normalizedNationalIdNumber =
                model.NationalIdNumber.Trim();

            // Prevent another borrower from using
            // the same identification type and number.
            var duplicateIdentification = await _context.Borrowers
                .AnyAsync(b =>
                    b.IdentificationType == borrowerIdentificationType &&
                    b.NationalIdNumber == normalizedNationalIdNumber &&
                    b.ApplicationUserId != userId
                );

            if (duplicateIdentification)
            {
                ModelState.AddModelError(
                    nameof(model.NationalIdNumber),
                    "This identification number is already registered."
                );

                return View("Step1", model);
            }

            // Create borrower record if it does not exist.
            if (borrower == null)
            {
                borrower = new Borrower
                {
                    ApplicationUserId = userId,

                    BorrowerNumber =
                        await GenerateBorrowerNumberAsync(),

                    // Temporary values for required database columns.
                    PhoneNumber = string.Empty,
                    County = string.Empty,

                    KycStatus = LendingEnums.KycStatus.NotStarted,

                    IsActive = true,
                    IsProfileComplete = false,
                    ProfileCompletionStep = 1
                };

                _context.Borrowers.Add(borrower);
            }

            // Save personal information.
            borrower.FirstName = model.FirstName.Trim();
            borrower.MiddleName = model.MiddleName?.Trim();
            borrower.LastName = model.LastName.Trim();

            // DateOnly? ViewModel 
            borrower.DateOfBirth = model.DateOfBirth!.Value;

            borrower.Gender = model.Gender;

            borrower.IdentificationType =
                borrowerIdentificationType;

            borrower.NationalIdNumber =
                normalizedNationalIdNumber;

            // Move borrower to Step 2.
            borrower.ProfileCompletionStep =
                Math.Max(borrower.ProfileCompletionStep, 2);

            // Any profile changes require review again.
            borrower.IsProfileComplete = false;
            borrower.ProfileCompletedAt = null;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Personal identification details saved successfully.";

            return RedirectToAction(nameof(Step2));
        }

        // =========================================================
        // STEP 2: CONTACT AND RESIDENTIAL INFORMATION
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Step2()
        {
            var borrower = await GetCurrentBorrowerAsync();

            if (borrower == null)
            {
                return RedirectToAction(nameof(Step1));
            }

            if (!CanAccessStep(borrower, 2))
            {
                return RedirectToStep(
                    borrower.ProfileCompletionStep
                );
            }

            var model = BuildViewModel(borrower);
            model.CurrentStep = 2;

            return View("Step2", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Step2(
            CreateProfileViewModel model)
        {
            model.CurrentStep = 2;

            var borrower = await GetCurrentBorrowerAsync();

            if (borrower == null)
            {
                return RedirectToAction(nameof(Step1));
            }

            if (!CanAccessStep(borrower, 2))
            {
                return RedirectToStep(
                    borrower.ProfileCompletionStep
                );
            }

            ValidateProperties(
                model,
                nameof(model.PhoneNumber),
                nameof(model.AlternativePhoneNumber),
                nameof(model.Email),
                nameof(model.County),
                nameof(model.SubCounty),
                nameof(model.Ward),
                nameof(model.EstateOrVillage),
                nameof(model.ResidenceType),
                nameof(model.PhysicalAddress)
            );

            KeepModelStateForProperties(
                nameof(model.PhoneNumber),
                nameof(model.AlternativePhoneNumber),
                nameof(model.Email),
                nameof(model.County),
                nameof(model.SubCounty),
                nameof(model.Ward),
                nameof(model.EstateOrVillage),
                nameof(model.ResidenceType),
                nameof(model.PhysicalAddress)
            );

            if (!ModelState.IsValid)
            {
                return View("Step2", model);
            }

            borrower.PhoneNumber =
                model.PhoneNumber.Trim();

            borrower.AlternativePhoneNumber =
                model.AlternativePhoneNumber?.Trim();

            borrower.Email =
                model.Email.Trim();

            borrower.County =
                model.County.Trim();

            borrower.SubCounty =
                model.SubCounty.Trim();

            borrower.Ward =
                model.Ward.Trim();

            borrower.EstateOrVillage =
                model.EstateOrVillage.Trim();

            borrower.ResidenceType =
                model.ResidenceType;

            borrower.PhysicalAddress =
                model.PhysicalAddress.Trim();

            borrower.ProfileCompletionStep =
                Math.Max(borrower.ProfileCompletionStep, 3);

            borrower.IsProfileComplete = false;
            borrower.ProfileCompletedAt = null;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Contact and residential information saved successfully.";

            return RedirectToAction(nameof(Step3));
        }

        // =========================================================
        // STEP 3: EMPLOYMENT AND INCOME
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Step3()
        {
            var borrower = await GetCurrentBorrowerAsync();

            if (borrower == null)
            {
                return RedirectToAction(nameof(Step1));
            }

            if (!CanAccessStep(borrower, 3))
            {
                return RedirectToStep(
                    borrower.ProfileCompletionStep
                );
            }

            var model = BuildViewModel(borrower);
            model.CurrentStep = 3;

            return View("Step3", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Step3(
            CreateProfileViewModel model)
        {
            model.CurrentStep = 3;

            var borrower = await GetCurrentBorrowerAsync();

            if (borrower == null)
            {
                return RedirectToAction(nameof(Step1));
            }

            if (!CanAccessStep(borrower, 3))
            {
                return RedirectToStep(
                    borrower.ProfileCompletionStep
                );
            }

            ValidateProperties(
                model,
                nameof(model.EmploymentStatus),
                nameof(model.Occupation),
                nameof(model.Industry),
                nameof(model.EmployerOrBusinessName),
                nameof(model.EmploymentContractType),
                nameof(model.EmploymentStartDate),
                nameof(model.BusinessYearsInOperation),
                nameof(model.GrossMonthlySalary),
                nameof(model.NetMonthlySalary),
                nameof(model.OtherMonthlyIncome),
                nameof(model.OtherIncomeSource),
                nameof(model.PayrollFrequency)
            );

            ValidateEmploymentInformation(model);

            KeepModelStateForProperties(
                nameof(model.EmploymentStatus),
                nameof(model.Occupation),
                nameof(model.Industry),
                nameof(model.EmployerOrBusinessName),
                nameof(model.EmploymentContractType),
                nameof(model.EmploymentStartDate),
                nameof(model.BusinessYearsInOperation),
                nameof(model.GrossMonthlySalary),
                nameof(model.NetMonthlySalary),
                nameof(model.OtherMonthlyIncome),
                nameof(model.OtherIncomeSource),
                nameof(model.PayrollFrequency)
            );

            if (!ModelState.IsValid)
            {
                return View("Step3", model);
            }

            borrower.EmploymentStatus =
                model.EmploymentStatus;

            borrower.Occupation =
                model.Occupation?.Trim();

            borrower.JobTitle =
                model.Occupation?.Trim();

            borrower.Industry =
                model.Industry?.Trim();

            borrower.EmployerOrBusinessName =
                model.EmployerOrBusinessName?.Trim();

            borrower.EmploymentContractType =
                model.EmploymentContractType;

            borrower.EmploymentStartDate =
                model.EmploymentStartDate;

            borrower.BusinessYearsInOperation =
                model.BusinessYearsInOperation;

            borrower.GrossMonthlySalary =
                model.GrossMonthlySalary;

            borrower.NetMonthlySalary =
                model.NetMonthlySalary;

            borrower.OtherMonthlyIncome =
                model.OtherMonthlyIncome;

            borrower.OtherIncomeSource =
                model.OtherIncomeSource?.Trim();

            borrower.PayrollFrequency =
                model.PayrollFrequency;

            // Synchronize the legacy EmploymentType property
            // when the enum members have matching names.
            var employmentStatusName =
                model.EmploymentStatus?.ToString();

            if (!string.IsNullOrWhiteSpace(employmentStatusName) &&
                Enum.TryParse<LendingEnums.EmploymentType>(
                    employmentStatusName,
                    true,
                    out var legacyEmploymentType
                ))
            {
                borrower.EmploymentType =
                    legacyEmploymentType;
            }

            borrower.ProfileCompletionStep =
                Math.Max(borrower.ProfileCompletionStep, 4);

            borrower.IsProfileComplete = false;
            borrower.ProfileCompletedAt = null;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Employment and income information saved successfully.";

            return RedirectToAction(nameof(Review));
        }

        // =========================================================
        // STEP 4: REVIEW PROFILE
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Review()
        {
            var borrower = await GetCurrentBorrowerAsync();

            if (borrower == null)
            {
                return RedirectToAction(nameof(Step1));
            }

            if (!CanAccessStep(borrower, 4))
            {
                return RedirectToStep(
                    borrower.ProfileCompletionStep
                );
            }

            var model = BuildViewModel(borrower);
            model.CurrentStep = 4;

            return View("Review", model);
        }

        // =========================================================
        // COMPLETE PROFILE
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Complete(
            CreateProfileViewModel model)
        {
            var borrower = await GetCurrentBorrowerAsync();

            if (borrower == null)
            {
                return RedirectToAction(nameof(Step1));
            }

            if (!CanAccessStep(borrower, 4))
            {
                return RedirectToStep(
                    borrower.ProfileCompletionStep
                );
            }

            // Validate saved database information rather than
            // trusting profile information posted by the browser.
            ModelState.Clear();

            var reviewModel = BuildViewModel(borrower);
            reviewModel.CurrentStep = 4;

            // Validate saved personal information.
            ValidateProperties(
                reviewModel,
                nameof(reviewModel.FirstName),
                nameof(reviewModel.MiddleName),
                nameof(reviewModel.LastName),
                nameof(reviewModel.DateOfBirth),
                nameof(reviewModel.Gender),
                nameof(reviewModel.IdentificationType),
                nameof(reviewModel.NationalIdNumber)
            );

            ValidateDateOfBirth(reviewModel);

            // Validate saved contact and residential information.
            ValidateProperties(
                reviewModel,
                nameof(reviewModel.PhoneNumber),
                nameof(reviewModel.AlternativePhoneNumber),
                nameof(reviewModel.Email),
                nameof(reviewModel.County),
                nameof(reviewModel.SubCounty),
                nameof(reviewModel.Ward),
                nameof(reviewModel.EstateOrVillage),
                nameof(reviewModel.ResidenceType),
                nameof(reviewModel.PhysicalAddress)
            );

            // Validate saved employment and income information.
            ValidateProperties(
                reviewModel,
                nameof(reviewModel.EmploymentStatus),
                nameof(reviewModel.Occupation),
                nameof(reviewModel.Industry),
                nameof(reviewModel.EmployerOrBusinessName),
                nameof(reviewModel.EmploymentContractType),
                nameof(reviewModel.EmploymentStartDate),
                nameof(reviewModel.BusinessYearsInOperation),
                nameof(reviewModel.GrossMonthlySalary),
                nameof(reviewModel.NetMonthlySalary),
                nameof(reviewModel.OtherMonthlyIncome),
                nameof(reviewModel.OtherIncomeSource),
                nameof(reviewModel.PayrollFrequency)
            );

            ValidateEmploymentInformation(reviewModel);

            // Validate declarations submitted from the Review page.
            if (!model.PrivacyNoticeAccepted)
            {
                ModelState.AddModelError(
                    nameof(model.PrivacyNoticeAccepted),
                    "You must acknowledge the privacy notice."
                );
            }

            if (!model.InformationAccurate)
            {
                ModelState.AddModelError(
                    nameof(model.InformationAccurate),
                    "You must confirm that the information provided is accurate."
                );
            }

            if (!ModelState.IsValid)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Your profile contains incomplete or invalid information. Please review the relevant sections."
                );

                reviewModel.PrivacyNoticeAccepted =
                    model.PrivacyNoticeAccepted;

                reviewModel.InformationAccurate =
                    model.InformationAccurate;

                return View("Review", reviewModel);
            }

            borrower.IsProfileComplete = true;
            borrower.ProfileCompletedAt = DateTime.UtcNow;
            borrower.ProfileCompletionStep = 4;

          

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Your borrower profile has been completed successfully.";

            return RedirectToAction("Index", "Dashboard");
        }

        // =========================================================
        // PROFILE INDEX
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var borrower = await GetCurrentBorrowerAsync();

            if (borrower == null)
            {
                return RedirectToAction(nameof(Step1));
            }

            if (borrower.IsProfileComplete)
            {
                return RedirectToAction(
                    "Index",
                    "Dashboard"
                );
            }

            return RedirectToStep(
                borrower.ProfileCompletionStep
            );
        }

        // =========================================================
        // EDIT PROFILE
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            var borrower = await GetCurrentBorrowerAsync();

            if (borrower == null)
            {
                return RedirectToAction(nameof(Step1));
            }

            // Editing starts again at Step 1.
            return RedirectToAction(nameof(Step1));
        }

        // =========================================================
        // VALIDATION HELPERS
        // =========================================================

        private void ValidateProperties(
            CreateProfileViewModel model,
            params string[] propertyNames)
        {
            var properties = typeof(CreateProfileViewModel)
                .GetProperties();

            foreach (var propertyName in propertyNames)
            {
                var property = properties.FirstOrDefault(
                    p => p.Name == propertyName
                );

                if (property == null)
                {
                    continue;
                }

                var value = property.GetValue(model);

                var validationContext = new ValidationContext(model)
                {
                    MemberName = propertyName
                };

                var validationResults =
                    new List<ValidationResult>();

                Validator.TryValidateProperty(
                    value,
                    validationContext,
                    validationResults
                );

                foreach (var result in validationResults)
                {
                    ModelState.AddModelError(
                        propertyName,
                        result.ErrorMessage ?? "Invalid value."
                    );
                }
            }
        }

        private void KeepModelStateForProperties(
            params string[] propertyNames)
        {
            var includedProperties = propertyNames.ToHashSet(
                StringComparer.Ordinal
            );

            foreach (var key in ModelState.Keys
                         .Where(key => !includedProperties.Contains(key))
                         .ToArray())
            {
                ModelState.Remove(key);
            }
        }

        // =========================================================
        // DATE OF BIRTH VALIDATION - DateOnly?
        // =========================================================

        private void ValidateDateOfBirth(
            CreateProfileViewModel model)
        {
            if (!model.DateOfBirth.HasValue)
            {
                return;
            }

            // Both values are DateOnly.
            var dateOfBirth = model.DateOfBirth.Value;
            var today = DateOnly.FromDateTime(DateTime.Today);

            if (dateOfBirth >= today)
            {
                ModelState.AddModelError(
                    nameof(model.DateOfBirth),
                    "Date of birth must be in the past."
                );

                return;
            }

            // Calculate age accurately.
            var age = today.Year - dateOfBirth.Year;

            if (dateOfBirth > today.AddYears(-age))
            {
                age--;
            }

            if (age < 18)
            {
                ModelState.AddModelError(
                    nameof(model.DateOfBirth),
                    "You must be at least 18 years old to register as a borrower."
                );
            }
        }

        // =========================================================
        // EMPLOYMENT VALIDATION
        // =========================================================

        private void ValidateEmploymentInformation(
            CreateProfileViewModel model)
        {
            var status =
                model.EmploymentStatus?.ToString() ?? "";

            bool IsFormalEmployee() =>
                status.Contains(
                    "Formal",
                    StringComparison.OrdinalIgnoreCase
                ) ||
                status.Equals(
                    "Employed",
                    StringComparison.OrdinalIgnoreCase
                );

            bool IsSelfEmployed() =>
                status.Contains(
                    "SelfEmployed",
                    StringComparison.OrdinalIgnoreCase
                ) ||
                status.Contains(
                    "Self_Employed",
                    StringComparison.OrdinalIgnoreCase
                );

            bool IsBusinessOwner() =>
                status.Contains(
                    "BusinessOwner",
                    StringComparison.OrdinalIgnoreCase
                ) ||
                status.Contains(
                    "Business_Owner",
                    StringComparison.OrdinalIgnoreCase
                );

            bool IsCasual() =>
                status.Contains(
                    "Casual",
                    StringComparison.OrdinalIgnoreCase
                );

            if (IsFormalEmployee())
            {
                Require(
                    model.EmployerOrBusinessName,
                    nameof(model.EmployerOrBusinessName),
                    "Employer name is required for formal employment."
                );

                Require(
                    model.Occupation,
                    nameof(model.Occupation),
                    "Occupation is required."
                );

                Require(
                    model.Industry,
                    nameof(model.Industry),
                    "Industry is required."
                );

                Require(
                    model.EmploymentContractType,
                    nameof(model.EmploymentContractType),
                    "Employment contract type is required."
                );

                Require(
                    model.EmploymentStartDate,
                    nameof(model.EmploymentStartDate),
                    "Employment start date is required."
                );

                RequirePositive(
                    model.GrossMonthlySalary,
                    nameof(model.GrossMonthlySalary),
                    "Gross monthly salary is required."
                );

                RequirePositive(
                    model.NetMonthlySalary,
                    nameof(model.NetMonthlySalary),
                    "Net monthly salary is required."
                );

                Require(
                    model.PayrollFrequency,
                    nameof(model.PayrollFrequency),
                    "Payroll frequency is required."
                );
            }
            else if (IsSelfEmployed() || IsBusinessOwner())
            {
                Require(
                    model.EmployerOrBusinessName,
                    nameof(model.EmployerOrBusinessName),
                    "Business name is required."
                );

                Require(
                    model.Occupation,
                    nameof(model.Occupation),
                    "Occupation is required."
                );

                Require(
                    model.Industry,
                    nameof(model.Industry),
                    "Industry is required."
                );

                Require(
                    model.BusinessYearsInOperation,
                    nameof(model.BusinessYearsInOperation),
                    "Years in business is required."
                );

                RequirePositive(
                    model.NetMonthlySalary,
                    nameof(model.NetMonthlySalary),
                    "Net monthly income is required."
                );
            }
            else if (IsCasual())
            {
                Require(
                    model.Occupation,
                    nameof(model.Occupation),
                    "Occupation is required."
                );

                RequirePositive(
                    model.NetMonthlySalary,
                    nameof(model.NetMonthlySalary),
                    "Net monthly income is required."
                );
            }

            // Other income requires a source.
            if (model.OtherMonthlyIncome.HasValue &&
                model.OtherMonthlyIncome.Value > 0 &&
                string.IsNullOrWhiteSpace(model.OtherIncomeSource))
            {
                ModelState.AddModelError(
                    nameof(model.OtherIncomeSource),
                    "Please specify the source of your other income."
                );
            }
        }

        private void Require(
            object? value,
            string propertyName,
            string errorMessage)
        {
            if (value == null ||
                (value is string text &&
                 string.IsNullOrWhiteSpace(text)))
            {
                ModelState.AddModelError(
                    propertyName,
                    errorMessage
                );
            }
        }

        private void RequirePositive(
            decimal? value,
            string propertyName,
            string errorMessage)
        {
            if (!value.HasValue || value.Value <= 0)
            {
                ModelState.AddModelError(
                    propertyName,
                    errorMessage
                );
            }
        }

        // =========================================================
        // VIEW MODEL MAPPING
        // =========================================================

        private CreateProfileViewModel BuildViewModel(
            Borrower borrower)
        {
            return new CreateProfileViewModel
            {
                FirstName = borrower.FirstName,
                MiddleName = borrower.MiddleName,
                LastName = borrower.LastName,

                // DateOnly? Entity -> DateOnly? ViewModel.
                DateOfBirth = borrower.DateOfBirth,

                Gender = borrower.Gender,

                // Entity identification enum -> ViewModel enum.
                IdentificationType =
                    Enum.Parse<Kredvanta.Models.Enums.IdentificationType>(
                        borrower.IdentificationType.ToString(),
                        ignoreCase: true
                    ),

                NationalIdNumber =
                    borrower.NationalIdNumber,

                PhoneNumber =
                    borrower.PhoneNumber,

                AlternativePhoneNumber =
                    borrower.AlternativePhoneNumber,

                Email =
                    borrower.Email,

                County =
                    borrower.County,

                SubCounty =
                    borrower.SubCounty,

                Ward =
                    borrower.Ward,

                EstateOrVillage =
                    borrower.EstateOrVillage,

                ResidenceType =
                    borrower.ResidenceType,

                PhysicalAddress =
                    borrower.PhysicalAddress,

                EmploymentStatus =
                    borrower.EmploymentStatus,

                Occupation =
                    borrower.Occupation,

                Industry =
                    borrower.Industry,

                EmployerOrBusinessName =
                    borrower.EmployerOrBusinessName,

                EmploymentContractType =
                    borrower.EmploymentContractType,

                EmploymentStartDate =
                    borrower.EmploymentStartDate,

                BusinessYearsInOperation =
                    borrower.BusinessYearsInOperation,

                GrossMonthlySalary =
                    borrower.GrossMonthlySalary,

                NetMonthlySalary =
                    borrower.NetMonthlySalary,

                OtherMonthlyIncome =
                    borrower.OtherMonthlyIncome,

                OtherIncomeSource =
                    borrower.OtherIncomeSource,

                PayrollFrequency =
                    borrower.PayrollFrequency
            };
        }

        // =========================================================
        // BORROWER RETRIEVAL
        // =========================================================

        private async Task<Borrower?> GetCurrentBorrowerAsync()
        {
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return null;
            }

            return await _context.Borrowers
                .FirstOrDefaultAsync(
                    b => b.ApplicationUserId == userId
                );
        }

        // =========================================================
        // STEP ACCESS CONTROL
        // =========================================================

        private bool CanAccessStep(
            Borrower borrower,
            int requestedStep)
        {
            // Completed borrowers may revisit earlier steps.
            if (borrower.IsProfileComplete)
            {
                return true;
            }

            return borrower.ProfileCompletionStep >= requestedStep;
        }

        private IActionResult RedirectToStep(int step)
        {
            return step switch
            {
                1 => RedirectToAction(nameof(Step1)),
                2 => RedirectToAction(nameof(Step2)),
                3 => RedirectToAction(nameof(Step3)),
                _ => RedirectToAction(nameof(Review))
            };
        }

        // =========================================================
        // BORROWER NUMBER GENERATION
        // =========================================================

        private async Task<string> GenerateBorrowerNumberAsync()
        {
            string borrowerNumber;

            do
            {
                var randomNumber = Random.Shared.Next(
                    100000,
                    999999
                );

                borrowerNumber = $"KRV{randomNumber}";

            }
            while (await _context.Borrowers.AnyAsync(
                b => b.BorrowerNumber == borrowerNumber
            ));

            return borrowerNumber;
        }
    }
}
