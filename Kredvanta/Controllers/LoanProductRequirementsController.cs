using Kredvanta.Authorization;
using Kredvanta.Data;
using Kredvanta.Models.Products;
using Kredvanta.Services.Audit;
using Kredvanta.ViewModels.LoanProducts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kredvanta.Controllers
{
    [Authorize(Roles = $"{AppRoles.Administrator},{AppRoles.CreditManager}")]
    public class LoanProductRequirementsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _audit;

        public LoanProductRequirementsController(ApplicationDbContext context, IAuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        public async Task<IActionResult> Index(Guid loanProductId)
        {
            var product = await _context.LoanProducts.AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == loanProductId);
            if (product == null) return NotFound();

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
            ViewBag.ProductCode = product.ProductCode;

            return View(requirements);
        }

        [HttpGet]
        public async Task<IActionResult> Create(Guid loanProductId)
        {
            var product = await _context.LoanProducts.AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == loanProductId);
            if (product == null) return NotFound();

            return View(new LoanProductRequirementViewModel
            {
                LoanProductId = product.Id,
                LoanProductName = product.ProductName,
                IsMandatory = true,
                IsActive = true
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(LoanProductRequirementViewModel model)
        {
            var product = await _context.LoanProducts.FirstOrDefaultAsync(p => p.Id == model.LoanProductId);
            if (product == null) return NotFound();

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

            _audit.Record("RequirementCreated", nameof(LoanProductRequiredDocument),
                requirement.Id.ToString(), null, new
                {
                    requirement.LoanProductId,
                    requirement.DocumentName,
                    requirement.IsMandatory,
                    requirement.IsActive
                });

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Document requirement added successfully.";
            return RedirectToAction(nameof(Index), new { loanProductId = product.Id });
        }

        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            var requirement = await _context.LoanProductRequiredDocuments
                .Include(d => d.LoanProduct)
                .FirstOrDefaultAsync(d => d.Id == id);
            if (requirement == null) return NotFound();

            return View("Create", new LoanProductRequirementViewModel
            {
                Id = requirement.Id,
                LoanProductId = requirement.LoanProductId,
                LoanProductName = requirement.LoanProduct.ProductName,
                DocumentName = requirement.DocumentName,
                Description = requirement.Description,
                RequiredPeriodMonths = requirement.RequiredPeriodMonths,
                IsMandatory = requirement.IsMandatory,
                IsActive = requirement.IsActive
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(LoanProductRequirementViewModel model)
        {
            var requirement = await _context.LoanProductRequiredDocuments
                .Include(d => d.LoanProduct)
                .FirstOrDefaultAsync(d => d.Id == model.Id && d.LoanProductId == model.LoanProductId);
            if (requirement == null) return NotFound();

            if (!ModelState.IsValid)
            {
                model.LoanProductName = requirement.LoanProduct.ProductName;
                return View("Create", model);
            }

            var before = new
            {
                requirement.DocumentName,
                requirement.RequiredPeriodMonths,
                requirement.IsMandatory,
                requirement.IsActive
            };

            requirement.DocumentName = model.DocumentName.Trim();
            requirement.Description = model.Description?.Trim();
            requirement.RequiredPeriodMonths = model.RequiredPeriodMonths;
            requirement.IsMandatory = model.IsMandatory;
            requirement.IsActive = model.IsActive;

            _audit.Record("RequirementUpdated", nameof(LoanProductRequiredDocument),
                requirement.Id.ToString(), null, new
                {
                    Before = before,
                    After = new
                    {
                        requirement.DocumentName,
                        requirement.RequiredPeriodMonths,
                        requirement.IsMandatory,
                        requirement.IsActive
                    }
                });

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Document requirement updated successfully.";
            return RedirectToAction(nameof(Index), new { loanProductId = requirement.LoanProductId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(Guid id, Guid loanProductId)
        {
            var requirement = await _context.LoanProductRequiredDocuments
                .FirstOrDefaultAsync(d => d.Id == id && d.LoanProductId == loanProductId);
            if (requirement == null) return NotFound();

            requirement.IsActive = !requirement.IsActive;

            _audit.Record("RequirementToggled", nameof(LoanProductRequiredDocument),
                requirement.Id.ToString(), null, new { requirement.IsActive });

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = requirement.IsActive
                ? "Document requirement activated successfully."
                : "Document requirement deactivated successfully.";

            return RedirectToAction(nameof(Index), new { loanProductId });
        }
    }
}