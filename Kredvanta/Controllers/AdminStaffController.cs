using System.Security.Claims;
using Kredvanta.Authorization;
using Kredvanta.Services.Staff;
using Kredvanta.ViewModels.Staff;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kredvanta.Controllers;

[Authorize(Roles = AppRoles.Administrator)]
[Route("admin/staff")]
public class AdminStaffController : Controller
{
    private readonly IStaffManagementService _staff;
    public AdminStaffController(IStaffManagementService staff) => _staff = staff;

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        ViewBag.CurrentUserId = UserId;
        return View(await _staff.ListAsync());
    }

    [HttpGet("create")]
    public IActionResult Create() => View(new CreateStaffViewModel());

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateStaffViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var r = await _staff.CreateAsync(model.Email, model.FirstName, model.LastName, model.Presets, UserId);
        if (!r.Succeeded)
        {
            ModelState.AddModelError(string.Empty, r.Error!);
            return View(model);
        }

        // Shown once on the next page, then discarded.
        TempData["NewStaffEmail"] = model.Email;
        TempData["NewStaffPassword"] = r.TempPassword;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("{id}/edit")]
    public async Task<IActionResult> Edit(string id)
    {
        var u = await _staff.GetAsync(id);
        if (u is null || u.IsAdministrator) return NotFound();

        return View(new EditStaffViewModel
        {
            Id = u.Id,
            Name = u.Name,
            Email = u.Email,
            Presets = StaffPresets.KeysFor(u.Roles).ToList()
        });
    }

    [HttpPost("{id}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, List<string> presets)
    {
        var r = await _staff.SetPresetsAsync(id, presets, UserId);
        TempData[r.Succeeded ? "Success" : "Error"] = r.Succeeded ? "Roles updated." : r.Error;
        return r.Succeeded ? RedirectToAction(nameof(Index)) : RedirectToAction(nameof(Edit), new { id });
    }

    [HttpPost("{id}/deactivate")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(string id)
    {
        var r = await _staff.SetActiveAsync(id, false, UserId);
        TempData[r.Succeeded ? "Success" : "Error"] = r.Succeeded ? "Account deactivated." : r.Error;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{id}/reactivate")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reactivate(string id)
    {
        var r = await _staff.SetActiveAsync(id, true, UserId);
        TempData[r.Succeeded ? "Success" : "Error"] = r.Succeeded ? "Account reactivated." : r.Error;
        return RedirectToAction(nameof(Index));
    }
}