using Kredvanta.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kredvanta.Controllers;

public class GoController : Controller
{
    [Authorize]
    [HttpGet("/go")]
    public IActionResult Index()
    {
        if (User.IsInRole(AppRoles.Administrator))
            return RedirectToAction("Index", "AdminDashboard");

        if (AppRoles.CaseReaders.Any(User.IsInRole))
            return RedirectToAction("Index", "StaffHome");

        return RedirectToAction("Index", "Dashboard");   // borrower
    }
}