using System.Security.Claims;
using Kredvanta.Authorization;
using Kredvanta.Data;
using Kredvanta.Models.Credit;
using Kredvanta.Services.Audit;
using Kredvanta.ViewModels.Credit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kredvanta.Controllers;

[Authorize(Roles = AppRoles.CreditManager)]
[Route("staff/credit-policies")]
public class CreditPolicyManagementController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IAuditService _audit;

    public CreditPolicyManagementController(ApplicationDbContext db, IAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var policies = await _db.CreditPolicyVersions
            .AsNoTracking()
            .OrderByDescending(policy => policy.VersionNumber)
            .ToListAsync(cancellationToken);
        return View(policies);
    }

    [HttpGet("create")]
    public IActionResult Create() => View(new CreditPolicyDraftViewModel());

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreditPolicyDraftViewModel model, CancellationToken cancellationToken)
    {
        if (model.EffectiveFrom > DateOnly.FromDateTime(DateTime.UtcNow))
            ModelState.AddModelError(nameof(model.EffectiveFrom), "A policy must not take effect in the future.");

        if (!ModelState.IsValid)
            return View(model);

        var versionNumber = (await _db.CreditPolicyVersions.MaxAsync(
            policy => (int?)policy.VersionNumber, cancellationToken) ?? 0) + 1;
        var policy = new CreditPolicyVersion
        {
            VersionNumber = versionNumber,
            Name = model.Name.Trim(),
            IsActive = false,
            EffectiveFrom = model.EffectiveFrom,
            MaxDebtServiceRatio = model.MaxDebtServiceRatio,
            MinNetMonthlyIncome = model.MinNetMonthlyIncome,
            MinDisposableAfterInstalment = model.MinDisposableAfterInstalment,
            DualApprovalAbove = model.DualApprovalAbove,
            CalculationVersion = model.CalculationVersion.Trim(),
            Notes = model.Notes?.Trim()
        };

        _db.CreditPolicyVersions.Add(policy);
        _audit.Record("CreditPolicyDraftCreated", nameof(CreditPolicyVersion), policy.Id.ToString(),
            $"Credit policy version {versionNumber} created as an inactive draft.");
        await _db.SaveChangesAsync(cancellationToken);

        TempData["Success"] = $"Policy version {versionNumber} saved as a draft. Activate it after confirming formal approval.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{id:guid}/activate")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
    {
        var policy = await _db.CreditPolicyVersions
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (policy is null)
            return NotFound();

        if (policy.EffectiveFrom > DateOnly.FromDateTime(DateTime.UtcNow))
        {
            TempData["Error"] = "This policy cannot be activated before its effective date.";
            return RedirectToAction(nameof(Index));
        }

        if (policy.IsActive)
        {
            TempData["Success"] = "This policy is already active.";
            return RedirectToAction(nameof(Index));
        }

        var previous = await _db.CreditPolicyVersions
            .FirstOrDefaultAsync(item => item.IsActive, cancellationToken);
        if (previous is not null)
            previous.IsActive = false;

        policy.IsActive = true;
        policy.ApprovedBy = User.Identity?.Name
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("The authenticated credit manager has no user identifier.");

        _audit.Record("CreditPolicyActivated", nameof(CreditPolicyVersion), policy.Id.ToString(),
            $"Credit manager activated policy version {policy.VersionNumber}.", new
            {
                PreviousVersion = previous?.VersionNumber,
                policy.VersionNumber,
                policy.ApprovedBy
            });
        await _db.SaveChangesAsync(cancellationToken);

        TempData["Success"] = $"Policy version {policy.VersionNumber} is now active.";
        return RedirectToAction(nameof(Index));
    }
}
