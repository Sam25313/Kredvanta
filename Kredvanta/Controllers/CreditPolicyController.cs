using Kredvanta.Authorization;
using Kredvanta.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kredvanta.Controllers;

public class CreditPolicyController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<CreditPolicyController> _logger;

    public CreditPolicyController(ApplicationDbContext db, ILogger<CreditPolicyController> logger)
    {
        _db = db;
        _logger = logger;
    }

    [AllowAnonymous]
    [HttpGet("/policy")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var publication = await _db.ApplicantCreditPolicyPublications
            .AsNoTracking()
            .Include(item => item.References)
            .OrderByDescending(item => item.IsPublished)
            .ThenByDescending(item => item.VersionNumber)
            .FirstOrDefaultAsync(cancellationToken);

        if (publication is null)
        {
            _logger.LogError("No applicant-facing credit policy publication is configured.");
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                "The applicant-facing credit policy is not available yet.");
        }

        ViewBag.Layout = User.IsInRole(AppRoles.Administrator)
            ? "_AdminLayout"
            : AppRoles.CaseReaders.Any(User.IsInRole) ? "_StaffLayout" : "_Layout";
        return View(publication);
    }
}
