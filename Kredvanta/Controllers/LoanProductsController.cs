
using Kredvanta.Data;
using Kredvanta.Models.Products;
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
            "Administrator,CreditManager,LoanOfficer";

        private const string ProductViewerRoles =
            "Administrator,CreditManager,LoanOfficer,Borrower";

        private readonly ApplicationDbContext _context;

        public LoanProductsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================================================
        // PRODUCT CATALOGUE
        // =========================================================

        [HttpGet]
        [Authorize(Roles = ProductViewerRoles)]
        public async Task<IActionResult> Index()
        {
            var query = _context.LoanProducts
                .AsNoTracking();

            // Borrowers can see active products only.
            // Application availability is handled separately in the view.
            if (User.IsInRole("Borrower"))
            {
                query = query.Where(p => p.IsActive);
            }

            var products = await query
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return View(products);
        }

        // =========================================================
        // PRODUCT DETAILS
        // =========================================================

        [HttpGet]
        [Authorize(Roles = ProductViewerRoles)]
        public async Task<IActionResult> Details(Guid id)
        {
            var query = _context.LoanProducts
                .AsNoTracking()
                .Include(p => p.RequiredDocuments)
                .AsQueryable();

            if (User.IsInRole("Borrower"))
            {
                query = query.Where(p => p.IsActive);
            }

            var product = await query
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // =========================================================
        // CREATE PRODUCT
        // =========================================================

        [HttpGet]
        [Authorize(Roles = StaffRoles)]
        public IActionResult Create()
        {
            return View(new LoanProductFormViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = StaffRoles)]
        public async Task<IActionResult> Create(
            LoanProductFormViewModel model)
        {
            ValidateProduct(model);

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var productCode = model.ProductCode
                .Trim()
                .ToUpperInvariant();

            var codeExists = await _context.LoanProducts
                .AnyAsync(p => p.ProductCode == productCode);

            if (codeExists)
            {
                ModelState.AddModelError(
                    nameof(model.ProductCode),
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
                IsAvailableForApplication =
                    model.IsAvailableForApplication
            };

            _context.LoanProducts.Add(product);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Loan product created successfully.";

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // EDIT PRODUCT
        // =========================================================

        [HttpGet]
        [Authorize(Roles = StaffRoles)]
        public async Task<IActionResult> Edit(Guid id)
        {
            var product = await _context.LoanProducts
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            var model = new LoanProductFormViewModel
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

                ProcessingFeePercentage =
                    product.ProcessingFeePercentage,

                ProcessingFeeFixed = product.ProcessingFeeFixed,
                GracePeriodDays = product.GracePeriodDays,

                IsActive = product.IsActive,
                IsAvailableForApplication =
                    product.IsAvailableForApplication
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = StaffRoles)]
        public async Task<IActionResult> Edit(
            Guid id,
            LoanProductFormViewModel model)
        {
            ValidateProduct(model);

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var product = await _context.LoanProducts
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            var productCode = model.ProductCode
                .Trim()
                .ToUpperInvariant();

            var codeExists = await _context.LoanProducts
                .AnyAsync(p =>
                    p.ProductCode == productCode &&
                    p.Id != id);

            if (codeExists)
            {
                ModelState.AddModelError(
                    nameof(model.ProductCode),
                    "Another product already uses this code.");

                return View(model);
            }

            product.ProductCode = productCode;
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

            product.ProcessingFeePercentage =
                model.ProcessingFeePercentage;

            product.ProcessingFeeFixed = model.ProcessingFeeFixed;
            product.GracePeriodDays = model.GracePeriodDays;

            product.IsActive = model.IsActive;
            product.IsAvailableForApplication =
                model.IsAvailableForApplication;

            product.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Loan product updated successfully.";

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // DEACTIVATE PRODUCT
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = StaffRoles)]
        public async Task<IActionResult> Deactivate(Guid id)
        {
            var product = await _context.LoanProducts
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            if (!product.IsActive)
            {
                TempData["InfoMessage"] =
                    "This loan product is already inactive.";

                return RedirectToAction(nameof(Index));
            }

            product.IsActive = false;

            // Also prevent new applications.
            product.IsAvailableForApplication = false;

            product.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Loan product deactivated successfully.";

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // PRODUCT VALIDATION
        // =========================================================

        private void ValidateProduct(LoanProductFormViewModel model)
        {
            if (model.MinimumAmount > model.MaximumAmount)
            {
                ModelState.AddModelError(
                    nameof(model.MaximumAmount),
                    "Maximum amount must be greater than or equal to minimum amount.");
            }

            if (model.MinimumTermPeriods > model.MaximumTermPeriods)
            {
                ModelState.AddModelError(
                    nameof(model.MaximumTermPeriods),
                    "Maximum repayment period must be greater than or equal to minimum period.");
            }

            if (string.IsNullOrWhiteSpace(model.Category))
            {
                ModelState.AddModelError(
                    nameof(model.Category),
                    "Please specify the product category.");
            }
        }
    }
}
