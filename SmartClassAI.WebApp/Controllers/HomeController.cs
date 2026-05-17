using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SmartClassAI.Models;
using SmartClassAI.WebApp.Models;
using SmartClassAI.WebApp.Utility;
using System.Diagnostics;

namespace SmartClassAI.WebApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public HomeController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        // =========================
        // 🏠 HOME PAGE
        // =========================
        public async Task<IActionResult> Index()
        {
            // ✅ IF USER LOGGED IN
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var user = await _userManager.GetUserAsync(User);

                if (user != null)
                {
                    // 👑 ADMIN
                    if (await _userManager.IsInRoleAsync(user, SD.Role_Admin))
                    {
                        return RedirectToAction(
                            "Index",
                            "Admin");
                    }

                    if (await _userManager.IsInRoleAsync(user, SD.Role_Teacher))
                        return RedirectToAction("Teacher", "Dashboard");

                    if (await _userManager.IsInRoleAsync(user, SD.Role_Student))
                        return RedirectToAction("Student", "Dashboard");

                    return RedirectToAction("Index", "Classroom");
                }
            }

            // 🌐 NORMAL LANDING PAGE
            return View();
        }

        // =========================
        // 🔒 PRIVACY
        // =========================
        public IActionResult Privacy()
        {
            return View();
        }

        // =========================
        // ❌ ERROR
        // =========================
        [ResponseCache(
            Duration = 0,
            Location = ResponseCacheLocation.None,
            NoStore = true)]
        public IActionResult Error()
        {
            return View(
                new ErrorViewModel
                {
                    RequestId =
                        Activity.Current?.Id ??
                        HttpContext.TraceIdentifier
                });
        }
    }
}