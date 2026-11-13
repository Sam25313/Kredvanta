
using Kredvanta.Data;
using Kredvanta.Models.Identity;
using Kredvanta.Models.Products;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kredvanta.Controllers
{
    [Authorize(Roles = "Borrower")]
    public class AvailableLoansController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AvailableLoansController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /AvailableLoans
        // Display all active loan products.
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            if (!await HasBorrowerProfileAsync())
                return RedirectToAction("Step1", "Profile");

            var products = await _context.LoanProducts
                .AsNoTracking()
                .Where(p => p.IsActive)
                .OrderBy(p => p.ProductName)
                .ToListAsync();

            return View(products);
        }

        // GET: /AvailableLoans/Details/{id}
        // Display an individual loan product and its requirements.
        [HttpGet]
        public async Task<IActionResult> Details(Guid id)
        {
            if (!await HasBorrowerProfileAsync())
                return RedirectToAction("Step1", "Profile");

            var product = await _context.LoanProducts
                .AsNoTracking()
                .Include(p => p.RequiredDocuments
                    .Where(d => d.IsActive))
                .FirstOrDefaultAsync(p =>
                    p.Id == id && p.IsActive);

            if (product == null)
                return NotFound();

            return View(product);
        }

        private async Task<bool> HasBorrowerProfileAsync()
        {
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(userId))
                return false;

            return await _context.Borrowers
                .AnyAsync(b => b.ApplicationUserId == userId);
        }
    }
}
