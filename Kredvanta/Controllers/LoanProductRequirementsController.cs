
using Kredvanta.Data;
using Kredvanta.Models.Products;
using Kredvanta.ViewModels.LoanProducts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kredvanta.Controllers
{
    [Authorize(Roles = "Administrator,CreditManager,LoanOfficer")]
    public class LoanProductRequirementsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public LoanProductRequirementsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: LoanProductRequirements?loanProductId=...
        public async Task<IActionResult> Index(Guid loanProductId)
        {
            var product = await _context.LoanProducts
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == loanProductId);

            if (product == null)
                return NotFound();

            var requirements = await _context.LoanProductRequiredDocuments
                .AsNoTracking()
                .Where(d => d.LoanProductId == loanProductId)
                .OrderBy(d => d.DocumentName)
                .Select(d => new LoanProductRequirementViewModel
                {
                    Id = d.Id,
                    LoanProductId = d.LoanProductId,
                    DocumentName = d.DocumentName,
                    Description = d.Description,
                    RequiredPeriodMonths = d.RequiredPeriodMonths,
                    IsMandatory = d.IsMandatory,
                    IsActive = d.IsActive
                })
                .ToListAsync();

            ViewBag.LoanProductId = product.Id;
            ViewBag.LoanProductName = product.ProductName;

            return View(requirements);
        }

        // GET: LoanProductRequirements/Create?loanProductId=...
        [HttpGet]
        public async Task<IActionResult> Create(Guid loanProductId)
        {
            var product = await _context.LoanProducts
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == loanProductId);

            if (product == null)
                return NotFound();

            return View(new LoanProductRequirementViewModel
            {
                LoanProductId = product.Id,
                LoanProductName = product.ProductName,
                IsMandatory = true,
                IsActive = true
            });
        }

        // POST: LoanProductRequirements/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            LoanProductRequirementViewModel model)
        {
            var product = await _context.LoanProducts
                .FirstOrDefaultAsync(p => p.Id == model.LoanProductId);

            if (product == null)
                return NotFound();

            if (!ModelState.IsValid)
            {
                model.LoanProductName = product.ProductName;
                return View(model);
            }

            var requirement = new LoanProductRequiredDocument
            {
                LoanProductId = product.Id,
                DocumentName = model.DocumentName.Trim(),
                Description = model.Description?.Trim(),
                RequiredPeriodMonths = model.RequiredPeriodMonths,
                IsMandatory = model.IsMandatory,
                IsActive = model.IsActive
            };

            _context.LoanProductRequiredDocuments.Add(requirement);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Document requirement added successfully.";

            return RedirectToAction(
                nameof(Index),
                new { loanProductId = product.Id });
        }

        // GET: LoanProductRequirements/Edit/...
        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            var requirement = await _context.LoanProductRequiredDocuments
                .Include(d => d.LoanProduct)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (requirement == null)
                return NotFound();

            var model = new LoanProductRequirementViewModel
            {
                Id = requirement.Id,
                LoanProductId = requirement.LoanProductId,
                LoanProductName = requirement.LoanProduct.ProductName,
                DocumentName = requirement.DocumentName,
                Description = requirement.Description,
                RequiredPeriodMonths = requirement.RequiredPeriodMonths,
                IsMandatory = requirement.IsMandatory,
                IsActive = requirement.IsActive
            };

            return View(model);
        }

        // POST: LoanProductRequirements/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            LoanProductRequirementViewModel model)
        {
            var requirement = await _context.LoanProductRequiredDocuments
                .Include(d => d.LoanProduct)
                .FirstOrDefaultAsync(d =>
                    d.Id == model.Id &&
                    d.LoanProductId == model.LoanProductId);

            if (requirement == null)
                return NotFound();

            if (!ModelState.IsValid)
            {
                model.LoanProductName = requirement.LoanProduct.ProductName;
                return View(model);
            }

            requirement.DocumentName = model.DocumentName.Trim();
            requirement.Description = model.Description?.Trim();
            requirement.RequiredPeriodMonths = model.RequiredPeriodMonths;
            requirement.IsMandatory = model.IsMandatory;
            requirement.IsActive = model.IsActive;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Document requirement updated successfully.";

            return RedirectToAction(
                nameof(Index),
                new { loanProductId = requirement.LoanProductId });
        }

        // POST: LoanProductRequirements/ToggleStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(
            Guid id,
            Guid loanProductId)
        {
            var requirement = await _context.LoanProductRequiredDocuments
                .FirstOrDefaultAsync(d =>
                    d.Id == id &&
                    d.LoanProductId == loanProductId);

            if (requirement == null)
                return NotFound();

            requirement.IsActive = !requirement.IsActive;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = requirement.IsActive
                ? "Document requirement activated successfully."
                : "Document requirement deactivated successfully.";

            return RedirectToAction(
                nameof(Index),
                new { loanProductId });
        }
    }
}
