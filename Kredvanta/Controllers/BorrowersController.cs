
using Kredvanta.Data;
using Kredvanta.Models.Borrowers;
using Kredvanta.ViewModels.Borrowers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kredvanta.Controllers
{
    [Authorize(Roles = "Administrator,LoanOfficer,CreditManager")]
    public class BorrowersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BorrowersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Borrowers
        public async Task<IActionResult> Index(
            string? search,
            int page = 1)
        {
            const int pageSize = 20;

            if (page < 1)
                page = 1;

            var query = _context.Borrowers
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                query = query.Where(b =>
                    b.FirstName.Contains(search) ||
                    b.LastName.Contains(search) ||
                    b.NationalIdNumber.Contains(search) ||
                    b.PhoneNumber.Contains(search) ||
                    b.BorrowerNumber.Contains(search));
            }

            var totalRecords = await query.CountAsync();

            var borrowers = await query
                .OrderBy(b => b.FirstName)
                .ThenBy(b => b.LastName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Search = search;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages =
                (int)Math.Ceiling(totalRecords / (double)pageSize);
            ViewBag.TotalRecords = totalRecords;

            return View(borrowers);
        }

        // GET: /Borrowers/Details/{id}
        public async Task<IActionResult> Details(Guid id)
        {
            var borrower = await _context.Borrowers
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == id);

            if (borrower == null)
                return NotFound();

            return View(borrower);
        }

        // GET: /Borrowers/Create
        public IActionResult Create()
        {
            return View(new BorrowerCreateViewModel());
        }

        // POST: /Borrowers/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            BorrowerCreateViewModel model)
        {
            if (model.DateOfBirth >=
                DateOnly.FromDateTime(DateTime.UtcNow))
            {
                ModelState.AddModelError(
                    nameof(model.DateOfBirth),
                    "Date of birth must be in the past.");
            }

            if (await _context.Borrowers.AnyAsync(
                b => b.NationalIdNumber == model.NationalIdNumber))
            {
                ModelState.AddModelError(
                    nameof(model.NationalIdNumber),
                    "A borrower with this National ID already exists.");
            }

            if (!ModelState.IsValid)
                return View(model);

            var borrower = new Borrower
            {
                BorrowerNumber = GenerateBorrowerNumber(),

                FirstName = model.FirstName.Trim(),
                MiddleName = model.MiddleName?.Trim(),
                LastName = model.LastName.Trim(),

                DateOfBirth = model.DateOfBirth,
                Gender = model.Gender,
                NationalIdNumber = model.NationalIdNumber.Trim(),

                PhoneNumber = model.PhoneNumber.Trim(),
                AlternativePhoneNumber =
                    model.AlternativePhoneNumber?.Trim(),
                Email = model.Email?.Trim(),

                County = model.County.Trim(),
                SubCounty = model.SubCounty?.Trim(),
                PhysicalAddress = model.PhysicalAddress?.Trim(),

                EmploymentType = model.EmploymentType,
                EmployerOrBusinessName =
                    model.EmployerOrBusinessName?.Trim(),
                MonthlyIncome = model.MonthlyIncome,

                IsActive = true
            };

            _context.Borrowers.Add(borrower);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Borrower registered successfully.";

            return RedirectToAction(nameof(Index));
        }

        // GET: /Borrowers/Edit/{id}
        public async Task<IActionResult> Edit(Guid id)
        {
            var borrower = await _context.Borrowers
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == id);

            if (borrower == null)
                return NotFound();

            var model = new BorrowerEditViewModel
            {
                Id = borrower.Id,
                FirstName = borrower.FirstName,
                MiddleName = borrower.MiddleName,
                LastName = borrower.LastName,
                DateOfBirth = borrower.DateOfBirth,
                Gender = borrower.Gender,
                NationalIdNumber = borrower.NationalIdNumber,
                PhoneNumber = borrower.PhoneNumber,
                AlternativePhoneNumber =
                    borrower.AlternativePhoneNumber,
                Email = borrower.Email,
                County = borrower.County,
                SubCounty = borrower.SubCounty,
                PhysicalAddress = borrower.PhysicalAddress,
                EmploymentType = borrower.EmploymentType,
                EmployerOrBusinessName =
                    borrower.EmployerOrBusinessName,
                MonthlyIncome = borrower.MonthlyIncome
            };

            return View(model);
        }

        // POST: /Borrowers/Edit/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            Guid id,
            BorrowerEditViewModel model)
        {
            if (id != model.Id)
                return BadRequest();

            if (model.DateOfBirth >=
                DateOnly.FromDateTime(DateTime.UtcNow))
            {
                ModelState.AddModelError(
                    nameof(model.DateOfBirth),
                    "Date of birth must be in the past.");
            }

            var duplicateNationalId =
                await _context.Borrowers.AnyAsync(b =>
                    b.NationalIdNumber == model.NationalIdNumber &&
                    b.Id != id);

            if (duplicateNationalId)
            {
                ModelState.AddModelError(
                    nameof(model.NationalIdNumber),
                    "Another borrower already uses this National ID.");
            }

            if (!ModelState.IsValid)
                return View(model);

            var borrower = await _context.Borrowers
                .FirstOrDefaultAsync(b => b.Id == id);

            if (borrower == null)
                return NotFound();

            borrower.FirstName = model.FirstName.Trim();
            borrower.MiddleName = model.MiddleName?.Trim();
            borrower.LastName = model.LastName.Trim();
            borrower.DateOfBirth = model.DateOfBirth;
            borrower.Gender = model.Gender;
            borrower.NationalIdNumber =
                model.NationalIdNumber.Trim();

            borrower.PhoneNumber = model.PhoneNumber.Trim();
            borrower.AlternativePhoneNumber =
                model.AlternativePhoneNumber?.Trim();
            borrower.Email = model.Email?.Trim();

            borrower.County = model.County.Trim();
            borrower.SubCounty = model.SubCounty?.Trim();
            borrower.PhysicalAddress =
                model.PhysicalAddress?.Trim();

            borrower.EmploymentType = model.EmploymentType;
            borrower.EmployerOrBusinessName =
                model.EmployerOrBusinessName?.Trim();
            borrower.MonthlyIncome = model.MonthlyIncome;

            borrower.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Borrower information updated successfully.";

            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: /Borrowers/Deactivate/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deactivate(Guid id)
        {
            var borrower = await _context.Borrowers
                .FirstOrDefaultAsync(b => b.Id == id);

            if (borrower == null)
                return NotFound();

            borrower.IsActive = false;
            borrower.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Borrower account deactivated successfully.";

            return RedirectToAction(nameof(Index));
        }

        private static string GenerateBorrowerNumber()
        {
            return $"BRW-{Guid.NewGuid().ToString("N")[..12].ToUpperInvariant()}";
        }
    }
}
