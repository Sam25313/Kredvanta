using System.Security.Claims;
using Kredvanta.Authorization;
using Kredvanta.Data;
using Kredvanta.Models.Products;
using Kredvanta.Services.Audit;
using Kredvanta.Services.Eligibility;
using Kredvanta.ViewModels.LoanProducts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kredvanta.Controllers
{
    [Authorize]
    public class LoanProductsController : Controller
    {
        private const string StaffRoles =
            $"{AppRoles.Administrator},{AppRoles.CreditManager}";

        private const string ProductViewerRoles =
            $"{AppRoles.Administrator},{AppRoles.CreditManager},{AppRoles.LoanOfficer}," +
            $"{AppRoles.Auditor},{AppRoles.Borrower}";

        private readonly ApplicationDbContext _context;
        private readonly IAuditService _audit;
        private readonly IEligibilityService _eligibility;

        public LoanProductsController(
            ApplicationDbContext context,
            IAuditService audit,
            IEligibilityService eligibility)
        {
            _context = context;
            _audit = audit;
            _eligibility = eligibility;
        }

        // ---------- Catalogue ----------

        [HttpGet]
        [Authorize(Roles = ProductViewerRoles)]
        public async Task<IActionResult> Index()
        {
            var query = _context.LoanProducts.AsNoTracking();

            if (User.IsInRole(AppRoles.Borrower))
                query = query.Where(p => p.IsActive && p.IsAvailableForApplication);

            var products = await query
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return View(products);
        }

        // ---------- Details ----------

        [HttpGet]
        [Authorize(Roles = ProductViewerRoles)]
        public async Task<IActionResult> Details(Guid id)
        {
            var query = _context.LoanProducts
                .AsNoTracking()
                .Include(p => p.RequiredDocuments)
                .AsQueryable();

            if (User.IsInRole(AppRoles.Borrower))
                query = query.Where(p => p.IsActive);

            var product = await query.FirstOrDefaultAsync(p => p.Id == id);
            if (product == null) return NotFound();

            // Display only. LoanApplications.Create and the workflow guards enforce the same rules.
            var reasons = new List<string>();

            if (!product.IsActive || !product.IsAvailableForApplication)
                reasons.Add("This product is not open for applications yet.");

            if (User.IsInRole(AppRoles.Borrower))
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var borrowerId = await _context.Borrowers
                    .Where(b => b.ApplicationUserId == userId && b.IsActive)
                    .Select(b => (Guid?)b.Id)
                    .FirstOrDefaultAsync();

                if (borrowerId is null)
                {
                    reasons.Add("Complete your applicant profile first.");
                }
                else
                {
                    var result = await _eligibility.CheckAsync(borrowerId.Value);
                    if (!result.IsEligible) reasons.AddRange(result.Reasons);
                }
            }
            else
            {
                reasons.Add("Sign in to your applicant account to apply.");
            }

            ViewBag.CanApply = reasons.Count == 0;
            ViewBag.ApplyBlockReasons = reasons;

            return View(product);
        }

        // ---------- Create ----------

        [HttpGet]
        [Authorize(Roles = StaffRoles)]
        public IActionResult Create() => View(new LoanProductFormViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = StaffRoles)]
        public async Task<IActionResult> Create(LoanProductFormViewModel model)
        {
            ValidateProduct(model);
            if (!ModelState.IsValid) return View(model);

            var productCode = model.ProductCode.Trim().ToUpperInvariant();

            if (await _context.LoanProducts.AnyAsync(p => p.ProductCode == productCode))
            {
                ModelState.AddModelError(nameof(model.ProductCode),
                    "A product with this code already exists.");
                return View(model);
            }

            var product = new LoanProduct
            {
                ProductCode = productCode,
                ProductName = model.ProductName.Trim(),
                Category = model.Category.Trim(),
                Description = model.Description?.Trim(),
                ImageUrl = model.ImageUrl?.Trim(),

                MinimumAmount = model.MinimumAmount,
                MaximumAmount = model.MaximumAmount,

                AnnualInterestRate = model.AnnualInterestRate,
                InterestMethod = model.InterestMethod,
                RepaymentFrequency = model.RepaymentFrequency,

                MinimumTermPeriods = model.MinimumTermPeriods,
                MaximumTermPeriods = model.MaximumTermPeriods,

                ProcessingFeePercentage = model.ProcessingFeePercentage,
                ProcessingFeeFixed = model.ProcessingFeeFixed,
                GracePeriodDays = model.GracePeriodDays,

                IsActive = model.IsActive,
                IsAvailableForApplication = model.IsAvailableForApplication
            };

            var defaultRequirements = LoanProductRequirementDefaults.AddMissingTo(product);
            _context.LoanProducts.Add(product);
            _context.LoanProductRequiredDocuments.AddRange(defaultRequirements);
            _audit.Record("ProductCreated", nameof(LoanProduct), product.Id.ToString(), null, Snapshot(product));

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Loan product created successfully.";
            return RedirectToAction(nameof(Index));
        }

        // ---------- Edit ----------

        [HttpGet]
        [Authorize(Roles = StaffRoles)]
        public async Task<IActionResult> Edit(Guid id)
        {
            var product = await _context.LoanProducts.AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);
            if (product == null) return NotFound();

            ViewBag.IsEdit = true;
            ViewBag.ProductId = product.Id;
            return View("Create", new LoanProductFormViewModel
            {
                ProductCode = product.ProductCode,
                ProductName = product.ProductName,
                Category = product.Category,
                Description = product.Description,
                ImageUrl = product.ImageUrl,

                MinimumAmount = product.MinimumAmount,
                MaximumAmount = product.MaximumAmount,

                AnnualInterestRate = product.AnnualInterestRate,
                InterestMethod = product.InterestMethod,
                RepaymentFrequency = product.RepaymentFrequency,

                MinimumTermPeriods = product.MinimumTermPeriods,
                MaximumTermPeriods = product.MaximumTermPeriods,

                ProcessingFeePercentage = product.ProcessingFeePercentage,
                ProcessingFeeFixed = product.ProcessingFeeFixed,
                GracePeriodDays = product.GracePeriodDays,

                IsActive = product.IsActive,
                IsAvailableForApplication = product.IsAvailableForApplication
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = StaffRoles)]
        public async Task<IActionResult> Edit(Guid id, LoanProductFormViewModel model)
        {
            var product = await _context.LoanProducts.FirstOrDefaultAsync(p => p.Id == id);
            if (product == null) return NotFound();

            // The product code is an identifier and is not editable after creation.
            model.ProductCode = product.ProductCode;
            ModelState.Remove(nameof(model.ProductCode));

            ValidateProduct(model);
            if (!ModelState.IsValid)
            {
                ViewBag.IsEdit = true;
                ViewBag.ProductId = product.Id;
                return View("Create", model);
            }

            var before = Snapshot(product);

            product.ProductName = model.ProductName.Trim();
            product.Category = model.Category.Trim();
            product.Description = model.Description?.Trim();
            product.ImageUrl = model.ImageUrl?.Trim();

            product.MinimumAmount = model.MinimumAmount;
            product.MaximumAmount = model.MaximumAmount;

            product.AnnualInterestRate = model.AnnualInterestRate;
            product.InterestMethod = model.InterestMethod;
            product.RepaymentFrequency = model.RepaymentFrequency;

            product.MinimumTermPeriods = model.MinimumTermPeriods;
            product.MaximumTermPeriods = model.MaximumTermPeriods;

            product.ProcessingFeePercentage = model.ProcessingFeePercentage;
            product.ProcessingFeeFixed = model.ProcessingFeeFixed;
            product.GracePeriodDays = model.GracePeriodDays;

            product.IsActive = model.IsActive;
            product.IsAvailableForApplication = model.IsAvailableForApplication;

            _audit.Record("ProductUpdated", nameof(LoanProduct), product.Id.ToString(), null,
                new { Before = before, After = Snapshot(product) });

            await _context.SaveChangesAsync();   // UpdatedAt is set by the DbContext

            TempData["SuccessMessage"] = "Loan product updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        // ---------- Toggle application availability ----------

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = StaffRoles)]
        public async Task<IActionResult> ToggleApplicationAvailability(Guid id)
        {
            var product = await _context.LoanProducts.FirstOrDefaultAsync(p => p.Id == id);
            if (product == null) return NotFound();

            if (!product.IsActive && !product.IsAvailableForApplication)
            {
                TempData["ErrorMessage"] = "Edit this product and activate it before opening applications.";
                return RedirectToAction(nameof(Index));
            }

            var before = product.IsAvailableForApplication;
            product.IsAvailableForApplication = !before;

            _audit.Record(product.IsAvailableForApplication ? "ProductApplicationsOpened" : "ProductApplicationsClosed",
                nameof(LoanProduct), product.Id.ToString(), null,
                new { product.ProductCode, Before = before, After = product.IsAvailableForApplication });

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = product.IsAvailableForApplication
                ? "Applications are now open for this product."
                : "Applications are now closed for this product.";
            return RedirectToAction(nameof(Index));
        }

        // ---------- Activate ----------

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = StaffRoles)]
        public async Task<IActionResult> Activate(Guid id)
        {
            var product = await _context.LoanProducts.FirstOrDefaultAsync(p => p.Id == id);
            if (product == null) return NotFound();

            if (product.IsActive)
            {
                TempData["InfoMessage"] = "This loan product is already active.";
                return RedirectToAction(nameof(Index));
            }

            product.IsActive = true;
            product.IsAvailableForApplication = false;

            _audit.Record("ProductActivated", nameof(LoanProduct), product.Id.ToString(), null,
                new { product.ProductCode, ApplicationsRemainClosed = true });

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Loan product activated. Applications remain closed until you open them separately.";
            return RedirectToAction(nameof(Index));
        }

        // ---------- Deactivate ----------

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = StaffRoles)]
        public async Task<IActionResult> Deactivate(Guid id)
        {
            var product = await _context.LoanProducts.FirstOrDefaultAsync(p => p.Id == id);
            if (product == null) return NotFound();

            if (!product.IsActive)
            {
                TempData["InfoMessage"] = "This loan product is already inactive.";
                return RedirectToAction(nameof(Index));
            }

            product.IsActive = false;
            product.IsAvailableForApplication = false;

            _audit.Record("ProductDeactivated", nameof(LoanProduct), product.Id.ToString(), null,
                new { product.ProductCode });

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Loan product deactivated successfully.";
            return RedirectToAction(nameof(Index));
        }

        // ---------- Helpers ----------

        private static object Snapshot(LoanProduct p) => new
        {
            p.ProductCode,
            p.ProductName,
            p.Category,
            p.MinimumAmount,
            p.MaximumAmount,
            p.AnnualInterestRate,
            InterestMethod = p.InterestMethod.ToString(),
            RepaymentFrequency = p.RepaymentFrequency.ToString(),
            p.MinimumTermPeriods,
            p.MaximumTermPeriods,
            p.ProcessingFeePercentage,
            p.ProcessingFeeFixed,
            p.GracePeriodDays,
            p.IsActive,
            p.IsAvailableForApplication
        };

        private void ValidateProduct(LoanProductFormViewModel model)
        {
            if (model.MinimumAmount > model.MaximumAmount)
                ModelState.AddModelError(nameof(model.MaximumAmount),
                    "Maximum amount must be greater than or equal to minimum amount.");

            if (model.MinimumTermPeriods > model.MaximumTermPeriods)
                ModelState.AddModelError(nameof(model.MaximumTermPeriods),
                    "Maximum repayment period must be greater than or equal to minimum period.");

            if (string.IsNullOrWhiteSpace(model.Category))
                ModelState.AddModelError(nameof(model.Category),
                    "Please specify the product category.");

            if (model.AnnualInterestRate < 0)
                ModelState.AddModelError(nameof(model.AnnualInterestRate),
                    "Interest rate cannot be negative.");

            if (model.IsAvailableForApplication && !model.IsActive)
                ModelState.AddModelError(nameof(model.IsAvailableForApplication),
                    "An inactive product cannot accept applications.");
        }
    }
}