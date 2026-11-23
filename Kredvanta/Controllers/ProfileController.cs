using Kredvanta.Authorization;
using Kredvanta.Data;
using Kredvanta.Models;
using Kredvanta.Models.Borrowers;
using Kredvanta.Models.Enums;
using Kredvanta.Models.Identity;
using Kredvanta.Services.Eligibility;
using Kredvanta.Services.Locations;
using Kredvanta.ViewModels.Profile;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Kredvanta.Controllers
{
    [Authorize(Roles = AppRoles.Borrower)]
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEligibilityService _eligibility;
        private readonly IKenyaLocationCatalog _locationCatalog;

        public ProfileController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IEligibilityService eligibility,
            IKenyaLocationCatalog locationCatalog)
        {
            _context = context;
            _userManager = userManager;
            _eligibility = eligibility;
            _locationCatalog = locationCatalog;
        }

        // Property groups per step (used for validation and ModelState filtering)
        private static readonly string[] Step1Properties =
        {
            nameof(CreateProfileViewModel.FirstName),
            nameof(CreateProfileViewModel.MiddleName),
            nameof(CreateProfileViewModel.LastName),
            nameof(CreateProfileViewModel.DateOfBirth),
            nameof(CreateProfileViewModel.Gender),
            nameof(CreateProfileViewModel.IdentificationType),
            nameof(CreateProfileViewModel.NationalIdNumber)
        };

        private static readonly string[] Step2Properties =
        {
            nameof(CreateProfileViewModel.PhoneNumber),
            nameof(CreateProfileViewModel.AlternativePhoneNumber),
            nameof(CreateProfileViewModel.Email),
            nameof(CreateProfileViewModel.County),
            nameof(CreateProfileViewModel.SubCounty),
            nameof(CreateProfileViewModel.Ward),
            nameof(CreateProfileViewModel.EstateOrVillage),
            nameof(CreateProfileViewModel.ResidenceType),
            nameof(CreateProfileViewModel.PhysicalAddress)
        };

        private static readonly string[] Step3Properties =
        {
            nameof(CreateProfileViewModel.EmploymentStatus),
            nameof(CreateProfileViewModel.Occupation),
            nameof(CreateProfileViewModel.Industry),
            nameof(CreateProfileViewModel.EmployerOrBusinessName),
            nameof(CreateProfileViewModel.EmploymentContractType),
            nameof(CreateProfileViewModel.EmploymentStartDate),
            nameof(CreateProfileViewModel.EmploymentEndDate),
            nameof(CreateProfileViewModel.ProbationEndDate),
            nameof(CreateProfileViewModel.BusinessYearsInOperation),
            nameof(CreateProfileViewModel.GrossMonthlySalary),
            nameof(CreateProfileViewModel.NetMonthlySalary),
            nameof(CreateProfileViewModel.OtherMonthlyIncome),
            nameof(CreateProfileViewModel.OtherIncomeSource),
            nameof(CreateProfileViewModel.PayrollFrequency)
        };

        // =========================================================
        // STEP 1: PERSONAL IDENTIFICATION
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Step1()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var borrower = await GetCurrentBorrowerAsync();
            var model = borrower == null
                ? new CreateProfileViewModel()
                : BuildViewModel(borrower);

            model.FirstName = user.FirstName;
            model.LastName = user.LastName;
            model.CurrentStep = 1;

            return View("Step1", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Step1(CreateProfileViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            model.FirstName = user.FirstName;
            model.LastName = user.LastName;
            ModelState.Remove(nameof(model.FirstName));
            ModelState.Remove(nameof(model.LastName));
            model.CurrentStep = 1;

            ValidateProperties(model, Step1Properties);
            ValidateDateOfBirth(model);
            KeepModelStateForProperties(Step1Properties);

            if (!ModelState.IsValid)
            {
                return View("Step1", model);
            }

            var userId = user.Id;
            var borrower = await GetCurrentBorrowerAsync();

            // View model and entity share the same enum.
            var identificationType = model.IdentificationType!.Value;
            var normalizedNationalIdNumber = model.NationalIdNumber.Trim();

            var duplicateIdentification = await _context.Borrowers
                .AnyAsync(b =>
                    b.IdentificationType == identificationType &&
                    b.NationalIdNumber == normalizedNationalIdNumber &&
                    b.ApplicationUserId != userId);

            if (duplicateIdentification)
            {
                ModelState.AddModelError(
                    nameof(model.NationalIdNumber),
                    "This identification number is already registered.");

                return View("Step1", model);
            }

            if (borrower == null)
            {
                borrower = new Borrower
                {
                    ApplicationUserId = userId,
                    BorrowerNumber = await GenerateBorrowerNumberAsync(),

                    PhoneNumber = user.PhoneNumber ?? string.Empty,
                    Email = user.Email,
                    County = string.Empty,

                    KycStatus = LendingEnums.KycStatus.NotStarted,
                    IsActive = true,
                    IsProfileComplete = false,
                    ProfileCompletionStep = 1
                };

                _context.Borrowers.Add(borrower);
            }

            borrower.FirstName = user.FirstName.Trim();
            borrower.MiddleName = model.MiddleName?.Trim();
            borrower.LastName = user.LastName.Trim();
            borrower.DateOfBirth = model.DateOfBirth!.Value;
            borrower.Gender = model.Gender;
            borrower.IdentificationType = identificationType;
            borrower.NationalIdNumber = normalizedNationalIdNumber;

            borrower.ProfileCompletionStep = Math.Max(borrower.ProfileCompletionStep, 2);

            // Any profile change requires the profile to be confirmed again.
            borrower.IsProfileComplete = false;
            borrower.ProfileCompletedAt = null;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Personal identification details saved successfully.";

            return RedirectToAction(nameof(Step2));
        }

        // =========================================================
        // STEP 2: CONTACT AND RESIDENTIAL INFORMATION
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Step2()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var borrower = await GetCurrentBorrowerAsync();

            if (borrower == null) return RedirectToAction(nameof(Step1));
            if (!CanAccessStep(borrower, 2)) return RedirectToStep(borrower.ProfileCompletionStep);

            var model = BuildViewModel(borrower);
            model.PhoneNumber = user.PhoneNumber ?? string.Empty;
            model.Email = user.Email ?? string.Empty;
            model.CurrentStep = 2;

            return View("Step2", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Step2(CreateProfileViewModel model)
        {
            model.CurrentStep = 2;

            var borrower = await GetCurrentBorrowerAsync();

            if (borrower == null) return RedirectToAction(nameof(Step1));
            if (!CanAccessStep(borrower, 2)) return RedirectToStep(borrower.ProfileCompletionStep);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            model.PhoneNumber = user.PhoneNumber ?? string.Empty;
            model.Email = user.Email ?? string.Empty;
            ModelState.Remove(nameof(model.PhoneNumber));
            ModelState.Remove(nameof(model.Email));

            ValidateProperties(model, Step2Properties);
            ValidateLocation(model);
            KeepModelStateForProperties(Step2Properties);

            if (!ModelState.IsValid)
            {
                return View("Step2", model);
            }

            borrower.PhoneNumber = user.PhoneNumber!.Trim();
            borrower.AlternativePhoneNumber = model.AlternativePhoneNumber?.Trim();
            borrower.Email = user.Email!.Trim();
            borrower.County = model.County.Trim();
            borrower.SubCounty = model.SubCounty?.Trim();
            borrower.Ward = model.Ward?.Trim();
            borrower.EstateOrVillage = model.EstateOrVillage?.Trim();
            borrower.ResidenceType = model.ResidenceType;
            borrower.PhysicalAddress = model.PhysicalAddress?.Trim();

            borrower.ProfileCompletionStep = Math.Max(borrower.ProfileCompletionStep, 3);
            borrower.IsProfileComplete = false;
            borrower.ProfileCompletedAt = null;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Contact and residential information saved successfully.";

            return RedirectToAction(nameof(Step3));
        }

        // =========================================================
        // STEP 3: EMPLOYMENT AND INCOME
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Step3()
        {
            var borrower = await GetCurrentBorrowerAsync();

            if (borrower == null) return RedirectToAction(nameof(Step1));
            if (!CanAccessStep(borrower, 3)) return RedirectToStep(borrower.ProfileCompletionStep);

            var model = BuildViewModel(borrower);
            model.CurrentStep = 3;

            return View("Step3", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Step3(CreateProfileViewModel model)
        {
            model.CurrentStep = 3;

            var borrower = await GetCurrentBorrowerAsync();

            if (borrower == null) return RedirectToAction(nameof(Step1));
            if (!CanAccessStep(borrower, 3)) return RedirectToStep(borrower.ProfileCompletionStep);

            // Drop dates that do not apply to the selected contract type.
            if (model.EmploymentContractType != EmploymentContractType.FixedTerm)
                model.EmploymentEndDate = null;

            if (model.EmploymentContractType != EmploymentContractType.Probationary)
                model.ProbationEndDate = null;

            ValidateProperties(model, Step3Properties);
            ValidateEmploymentInformation(model);
            KeepModelStateForProperties(Step3Properties);

            if (!ModelState.IsValid)
            {
                return View("Step3", model);
            }

            borrower.EmploymentStatus = model.EmploymentStatus;
            borrower.Occupation = model.Occupation?.Trim();
            borrower.JobTitle = model.Occupation?.Trim();
            borrower.Industry = model.Industry?.Trim();
            borrower.EmployerOrBusinessName = model.EmployerOrBusinessName?.Trim();
            borrower.EmploymentContractType = model.EmploymentContractType;
            borrower.EmploymentStartDate = model.EmploymentStartDate;
            borrower.EmploymentEndDate = model.EmploymentEndDate;
            borrower.ProbationEndDate = model.ProbationEndDate;
            borrower.BusinessYearsInOperation = model.BusinessYearsInOperation;
            borrower.GrossMonthlySalary = model.GrossMonthlySalary;
            borrower.NetMonthlySalary = model.NetMonthlySalary;
            borrower.OtherMonthlyIncome = model.OtherMonthlyIncome;
            borrower.OtherIncomeSource = model.OtherIncomeSource?.Trim();
            borrower.PayrollFrequency = model.PayrollFrequency;

            // Legacy field: explicit mapping. Never use it for eligibility.
            borrower.EmploymentType = model.EmploymentStatus switch
            {
                EmploymentStatus.FormallyEmployed => LendingEnums.EmploymentType.Employed,
                EmploymentStatus.SelfEmployed => LendingEnums.EmploymentType.SelfEmployed,
                EmploymentStatus.BusinessOwner => LendingEnums.EmploymentType.BusinessOwner,
                EmploymentStatus.CasualWorker => LendingEnums.EmploymentType.CasualWorker,
                EmploymentStatus.Unemployed => LendingEnums.EmploymentType.Unemployed,
                _ => LendingEnums.EmploymentType.Other
            };

            borrower.ProfileCompletionStep = Math.Max(borrower.ProfileCompletionStep, 4);
            borrower.IsProfileComplete = false;
            borrower.ProfileCompletedAt = null;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Employment and income information saved successfully.";

            return RedirectToAction(nameof(Review));
        }

        // =========================================================
        // STEP 4: REVIEW PROFILE
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Review()
        {
            var borrower = await GetCurrentBorrowerAsync();

            if (borrower == null) return RedirectToAction(nameof(Step1));
            if (!CanAccessStep(borrower, 4)) return RedirectToStep(borrower.ProfileCompletionStep);

            var model = BuildViewModel(borrower);
            model.CurrentStep = 4;

            return View("Review", model);
        }

        // =========================================================
        // COMPLETE PROFILE
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Complete(CreateProfileViewModel model)
        {
            var borrower = await GetCurrentBorrowerAsync();

            if (borrower == null) return RedirectToAction(nameof(Step1));
            if (!CanAccessStep(borrower, 4)) return RedirectToStep(borrower.ProfileCompletionStep);

            // Validate saved database information, not what the browser posted.
            ModelState.Clear();

            var reviewModel = BuildViewModel(borrower);
            reviewModel.CurrentStep = 4;

            ValidateProperties(reviewModel, Step1Properties);
            ValidateDateOfBirth(reviewModel);
            ValidateProperties(reviewModel, Step2Properties);
            ValidateProperties(reviewModel, Step3Properties);
            ValidateEmploymentInformation(reviewModel);

            if (!model.PrivacyNoticeAccepted)
            {
                ModelState.AddModelError(
                    nameof(model.PrivacyNoticeAccepted),
                    "You must acknowledge the privacy notice.");
            }

            if (!model.InformationAccurate)
            {
                ModelState.AddModelError(
                    nameof(model.InformationAccurate),
                    "You must confirm that the information provided is accurate.");
            }

            if (!ModelState.IsValid)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Your profile contains incomplete or invalid information. Please review the relevant sections.");

                reviewModel.PrivacyNoticeAccepted = model.PrivacyNoticeAccepted;
                reviewModel.InformationAccurate = model.InformationAccurate;

                return View("Review", reviewModel);
            }

            borrower.IsProfileComplete = true;
            borrower.ProfileCompletedAt = DateTime.UtcNow;
            borrower.ProfileCompletionStep = 4;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Your applicant profile has been completed successfully.";

            // Advisory only. EligibilityGuard is the real enforcement at submission.
            var eligibility = await _eligibility.CheckAsync(borrower.Id);
            if (!eligibility.IsEligible)
            {
                TempData["Warning"] = string.Join(" ", eligibility.Reasons);
            }

            return RedirectToAction("Index", "Dashboard");
        }

        // =========================================================
        // PROFILE INDEX
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var borrower = await GetCurrentBorrowerAsync();

            if (borrower == null) return RedirectToAction(nameof(Step1));

            if (borrower.IsProfileComplete)
            {
                return RedirectToAction("Index", "Dashboard");
            }

            return RedirectToStep(borrower.ProfileCompletionStep);
        }

        // =========================================================
        // EDIT PROFILE
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            var borrower = await GetCurrentBorrowerAsync();

            if (borrower == null) return RedirectToAction(nameof(Step1));

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
            var properties = typeof(CreateProfileViewModel).GetProperties();

            foreach (var propertyName in propertyNames)
            {
                var property = properties.FirstOrDefault(p => p.Name == propertyName);
                if (property == null) continue;

                var value = property.GetValue(model);

                var validationContext = new ValidationContext(model)
                {
                    MemberName = propertyName
                };

                var validationResults = new List<ValidationResult>();

                Validator.TryValidateProperty(value, validationContext, validationResults);

                foreach (var result in validationResults)
                {
                    ModelState.AddModelError(
                        propertyName,
                        result.ErrorMessage ?? "Invalid value.");
                }
            }
        }

        private void KeepModelStateForProperties(params string[] propertyNames)
        {
            var included = propertyNames.ToHashSet(StringComparer.Ordinal);

            foreach (var key in ModelState.Keys
                         .Where(key => !included.Contains(key))
                         .ToArray())
            {
                ModelState.Remove(key);
            }
        }

        private void ValidateLocation(CreateProfileViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.County)) return;

            if (!_locationCatalog.ContainsCounty(model.County))
            {
                ModelState.AddModelError(nameof(model.County), "Select a valid county.");
                return;
            }

            if (string.IsNullOrWhiteSpace(model.SubCounty)) return;

            if (!_locationCatalog.ContainsSubCounty(model.County, model.SubCounty))
            {
                ModelState.AddModelError(nameof(model.SubCounty), "Select a sub-county in the selected county.");
                return;
            }

            if (!string.IsNullOrWhiteSpace(model.Ward) &&
                !_locationCatalog.ContainsWard(model.County, model.SubCounty, model.Ward))
            {
                ModelState.AddModelError(nameof(model.Ward), "Select a ward in the selected sub-county.");
            }
        }

        private void ValidateDateOfBirth(CreateProfileViewModel model)
        {
            if (!model.DateOfBirth.HasValue) return;

            var dateOfBirth = model.DateOfBirth.Value;
            var today = DateOnly.FromDateTime(DateTime.Today);

            if (dateOfBirth >= today)
            {
                ModelState.AddModelError(
                    nameof(model.DateOfBirth),
                    "Date of birth must be in the past.");
                return;
            }

            var age = today.Year - dateOfBirth.Year;

            if (dateOfBirth > today.AddYears(-age))
            {
                age--;
            }

            if (age < 18)
            {
                ModelState.AddModelError(
                    nameof(model.DateOfBirth),
                    "You must be at least 18 years old to register as an applicant.");
            }
        }

        private void ValidateEmploymentInformation(CreateProfileViewModel model)
        {
            switch (model.EmploymentStatus)
            {
                case EmploymentStatus.FormallyEmployed:
                    Require(model.EmployerOrBusinessName, nameof(model.EmployerOrBusinessName), "Employer name is required for formal employment.");
                    Require(model.Occupation, nameof(model.Occupation), "Occupation is required.");
                    Require(model.Industry, nameof(model.Industry), "Industry is required.");
                    Require(model.EmploymentContractType, nameof(model.EmploymentContractType), "Employment contract type is required.");
                    Require(model.EmploymentStartDate, nameof(model.EmploymentStartDate), "Employment start date is required.");
                    RequirePositive(model.GrossMonthlySalary, nameof(model.GrossMonthlySalary), "Gross monthly salary is required.");
                    RequirePositive(model.NetMonthlySalary, nameof(model.NetMonthlySalary), "Net monthly salary is required.");
                    Require(model.PayrollFrequency, nameof(model.PayrollFrequency), "Payroll frequency is required.");

                    if (model.GrossMonthlySalary > 0 && model.NetMonthlySalary > model.GrossMonthlySalary)
                    {
                        ModelState.AddModelError(
                            nameof(model.NetMonthlySalary),
                            "Net salary cannot exceed gross salary.");
                    }

                    if (model.EmploymentContractType == EmploymentContractType.FixedTerm)
                    {
                        Require(model.EmploymentEndDate, nameof(model.EmploymentEndDate),
                            "Contract end date is required for fixed-term contracts.");
                    }

                    if (model.EmploymentContractType == EmploymentContractType.Probationary)
                    {
                        Require(model.ProbationEndDate, nameof(model.ProbationEndDate),
                            "Probation end date is required.");
                    }

                    if (model.EmploymentEndDate is { } end &&
                        model.EmploymentStartDate is { } start &&
                        end <= start)
                    {
                        ModelState.AddModelError(
                            nameof(model.EmploymentEndDate),
                            "Contract end date must be after the start date.");
                    }
                    break;

                case EmploymentStatus.SelfEmployed:
                case EmploymentStatus.BusinessOwner:
                    Require(model.EmployerOrBusinessName, nameof(model.EmployerOrBusinessName), "Business name is required.");
                    Require(model.Occupation, nameof(model.Occupation), "Occupation is required.");
                    Require(model.Industry, nameof(model.Industry), "Industry is required.");
                    Require(model.BusinessYearsInOperation, nameof(model.BusinessYearsInOperation), "Years in business is required.");
                    RequirePositive(model.NetMonthlySalary, nameof(model.NetMonthlySalary), "Net monthly income is required.");
                    break;

                case EmploymentStatus.CasualWorker:
                    Require(model.Occupation, nameof(model.Occupation), "Occupation is required.");
                    RequirePositive(model.NetMonthlySalary, nameof(model.NetMonthlySalary), "Net monthly income is required.");
                    break;
            }

            if (model.OtherMonthlyIncome is > 0 &&
                string.IsNullOrWhiteSpace(model.OtherIncomeSource))
            {
                ModelState.AddModelError(
                    nameof(model.OtherIncomeSource),
                    "Please specify the source of your other income.");
            }
        }

        private void Require(object? value, string propertyName, string errorMessage)
        {
            if (value == null || (value is string text && string.IsNullOrWhiteSpace(text)))
            {
                ModelState.AddModelError(propertyName, errorMessage);
            }
        }

        private void RequirePositive(decimal? value, string propertyName, string errorMessage)
        {
            if (!value.HasValue || value.Value <= 0)
            {
                ModelState.AddModelError(propertyName, errorMessage);
            }
        }

        // =========================================================
        // VIEW MODEL MAPPING
        // =========================================================

        private static CreateProfileViewModel BuildViewModel(Borrower borrower)
        {
            return new CreateProfileViewModel
            {
                FirstName = borrower.FirstName,
                MiddleName = borrower.MiddleName,
                LastName = borrower.LastName,
                DateOfBirth = borrower.DateOfBirth,
                Gender = borrower.Gender,
                IdentificationType = borrower.IdentificationType,
                NationalIdNumber = borrower.NationalIdNumber,

                PhoneNumber = borrower.PhoneNumber,
                AlternativePhoneNumber = borrower.AlternativePhoneNumber,
                Email = borrower.Email ?? string.Empty,

                County = borrower.County,
                SubCounty = borrower.SubCounty,
                Ward = borrower.Ward,
                EstateOrVillage = borrower.EstateOrVillage,
                ResidenceType = borrower.ResidenceType,
                PhysicalAddress = borrower.PhysicalAddress,

                EmploymentStatus = borrower.EmploymentStatus,
                Occupation = borrower.Occupation,
                Industry = borrower.Industry,
                EmployerOrBusinessName = borrower.EmployerOrBusinessName,
                EmploymentContractType = borrower.EmploymentContractType,
                EmploymentStartDate = borrower.EmploymentStartDate,
                EmploymentEndDate = borrower.EmploymentEndDate,
                ProbationEndDate = borrower.ProbationEndDate,
                BusinessYearsInOperation = borrower.BusinessYearsInOperation,

                GrossMonthlySalary = borrower.GrossMonthlySalary,
                NetMonthlySalary = borrower.NetMonthlySalary,
                OtherMonthlyIncome = borrower.OtherMonthlyIncome,
                OtherIncomeSource = borrower.OtherIncomeSource,
                PayrollFrequency = borrower.PayrollFrequency
            };
        }

        // =========================================================
        // BORROWER RETRIEVAL
        // =========================================================

        private async Task<Borrower?> GetCurrentBorrowerAsync()
        {
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(userId)) return null;

            return await _context.Borrowers
                .FirstOrDefaultAsync(b => b.ApplicationUserId == userId);
        }

        // =========================================================
        // STEP ACCESS CONTROL
        // =========================================================

        private static bool CanAccessStep(Borrower borrower, int requestedStep)
        {
            // Completed borrowers may revisit earlier steps.
            if (borrower.IsProfileComplete) return true;

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
                borrowerNumber = $"KRV{Random.Shared.Next(100000, 999999)}";
            }
            while (await _context.Borrowers.AnyAsync(b => b.BorrowerNumber == borrowerNumber));

            return borrowerNumber;
        }
    }
}