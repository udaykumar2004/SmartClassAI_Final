using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SmartClassAI.Models;
using SmartClassAI.WebApp.Services;
using SmartClassAI.WebApp.Utility;

namespace SmartClassAI.WebApp.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly DashboardService _dashboard;
        private readonly UserManager<ApplicationUser> _userManager;

        public DashboardController(
            DashboardService dashboard,
            UserManager<ApplicationUser> userManager)
        {
            _dashboard = dashboard;
            _userManager = userManager;
        }

        public async Task<IActionResult> Teacher()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            if (!await _userManager.IsInRoleAsync(user, SD.Role_Teacher)
                && !await _userManager.IsInRoleAsync(user, SD.Role_Admin))
                return Forbid();

            var stats = await _dashboard.GetTeacherStatsAsync(user.Id);
            return View(stats);
        }

        public async Task<IActionResult> Student()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var stats = await _dashboard.GetStudentStatsAsync(user.Id);
            return View(stats);
        }
    }
}
