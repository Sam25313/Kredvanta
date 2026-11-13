
using Kredvanta.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kredvanta.Controllers
{
    [Authorize(Roles = "Borrower")]
    public class BorrowerLoanProductsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BorrowerLoanProductsController(
            ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /BorrowerLoanProducts
        public async Task<IActionResult> Index()
        {
            var products = await _context.LoanProducts
         .AsNoTracking()
         .Where(p => p.IsActive && p.IsAvailableForApplication)
         .OrderBy(p => p.Category)
         .ThenBy(p => p.ProductName)
         .ToListAsync();

            return View(products);
        }

        // GET: /BorrowerLoanProducts/Details/{id}
        public async Task<IActionResult> Details(Guid id)
        {
            var product = await _context.LoanProducts
                .AsNoTracking()
                .Include(p => p.RequiredDocuments)
                .FirstOrDefaultAsync(p =>
                 p.Id == id &&
                 p.IsActive &&
                 p.IsAvailableForApplication);

            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }
    }
}
