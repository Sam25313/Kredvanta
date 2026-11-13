
using Kredvanta.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Kredvanta.Controllers
{
    public class HomeController : Controller
    {

        // GET: /
        // Redirect authenticated users according to their roles.
        // Visitors see the public homepage.

        [AllowAnonymous]
        public IActionResult Index()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                if (User.IsInRole("Administrator"))
                {
                    return RedirectToAction(
                        "Index", "AdminDashboard");
                }

                if (User.IsInRole("CreditManager"))
                {
                    return RedirectToAction(
                        "Index", "CreditDashboard");
                }

                if (User.IsInRole("LoanOfficer"))
                {
                    return RedirectToAction(
                        "Index", "LoanOfficerDashboard");
                }

                if (User.IsInRole("Borrower"))
                {
                    return Redirect("/BorrowerDashboard");
                }

                return Forbid();
            }

            return View();
        }

        // GET: /Home/Privacy
        [AllowAnonymous]
        public IActionResult Privacy()
        {
            return View();
        }



        // GET: /Home/Error
        [AllowAnonymous]
        [ResponseCache(
            Duration = 0,
            Location = ResponseCacheLocation.None,
            NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id
                    ?? HttpContext.TraceIdentifier
            });
        }
    }
}
