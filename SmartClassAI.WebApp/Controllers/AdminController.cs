using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SmartClassAI.DataAccess.Data;
using SmartClassAI.DataAccess.Repository.IRepository;
using SmartClassAI.Models;
using SmartClassAI.WebApp.Hubs;
using SmartClassAI.WebApp.Services;
using SmartClassAI.WebApp.Utility;

namespace SmartClassAI.WebApp.Controllers
{
    [Authorize(Roles = SD.Role_Admin)]
    public class AdminController : Controller
    {
        private readonly DashboardService _dashboardService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHubContext<AdminHub> _hub;

        public AdminController(
            DashboardService dashboardService,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context,
            IUnitOfWork unitOfWork,
            IHubContext<AdminHub> hub)
        {
            _dashboardService = dashboardService;
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _unitOfWork = unitOfWork;
            _hub = hub;
        }

        public IActionResult Index() => View();

        [HttpGet]
        public async Task<IActionResult> GetStats()
        {
            var data = await _dashboardService.GetStatsAsync();
            return Json(data);
        }

        [HttpGet]
        public async Task<IActionResult> GetActivities()
        {
            var logs = await _dashboardService.GetLatestActivitiesAsync(30);
            return Json(logs.Select(x => new
            {
                x.Message,
                time = x.CreatedAt.ToString("g")
            }));
        }

        public async Task<IActionResult> Users(string? search)
        {
            var query = _userManager.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();
                query = query.Where(u =>
                    u.Email!.Contains(search) ||
                    u.FullName.Contains(search));
            }

            var users = await query
                .OrderByDescending(u => u.CreatedAt)
                .Take(200)
                .ToListAsync();

            var vm = new List<AdminUserListItem>();
            foreach (var u in users)
            {
                var roles = await _userManager.GetRolesAsync(u);
                vm.Add(new AdminUserListItem
                {
                    User = u,
                    Roles = roles.ToList()
                });
            }

            ViewBag.Search = search;
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetRole(string userId, string role)
        {
            if (role is not (SD.Role_Admin or SD.Role_Teacher or SD.Role_Student))
                return BadRequest();

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            await _userManager.AddToRoleAsync(user, role);

            await LogActivityAsync($"Role changed to {role} for {user.Email}");
            TempData["success"] = "User role updated.";
            return RedirectToAction(nameof(Users));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleSuspend(string userId, string? reason)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            if (await _userManager.IsInRoleAsync(user, SD.Role_Admin))
            {
                TempData["error"] = "Cannot suspend an administrator.";
                return RedirectToAction(nameof(Users));
            }

            user.IsSuspended = !user.IsSuspended;
            user.SuspensionReason = user.IsSuspended ? reason : null;
            user.IsActive = !user.IsSuspended;

            await _userManager.UpdateAsync(user);
            await LogActivityAsync(
                $"{(user.IsSuspended ? "Suspended" : "Unsuspended")} user {user.Email}");

            TempData["success"] = user.IsSuspended
                ? "User suspended."
                : "User unsuspended.";
            return RedirectToAction(nameof(Users));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            if (await _userManager.IsInRoleAsync(user, SD.Role_Admin))
            {
                TempData["error"] = "Cannot delete an administrator.";
                return RedirectToAction(nameof(Users));
            }

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                await LogActivityAsync($"Deleted user {user.Email}");
                TempData["success"] = "User deleted.";
            }
            else
            {
                TempData["error"] = string.Join(", ", result.Errors.Select(e => e.Description));
            }

            return RedirectToAction(nameof(Users));
        }

        public async Task<IActionResult> Classrooms()
        {
            var list = await _unitOfWork.Classroom.GetAllAsync(includeProperties: "Teacher");
            return View(list.OrderByDescending(c => c.CreatedAt).ToList());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteClassroom(int id)
        {
            var classroom = await _unitOfWork.Classroom.GetAsync(x => x.Id == id);
            if (classroom == null) return NotFound();

            _unitOfWork.Classroom.Remove(classroom);
            await _unitOfWork.SaveAsync();

            await LogActivityAsync($"Deleted classroom: {classroom.Name}");
            TempData["success"] = "Classroom deleted.";
            return RedirectToAction(nameof(Classrooms));
        }

        public async Task<IActionResult> AuditLogs()
        {
            var logs = await _dashboardService.GetLatestActivitiesAsync(100);
            return View(logs);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PushStats()
        {
            var data = await _dashboardService.GetStatsAsync();
            await _hub.Clients.All.SendAsync("ReceiveDashboardUpdate", data);
            await _hub.Clients.All.SendAsync("ReceiveActivity", "Dashboard stats updated");
            return Ok(data);
        }

        private async Task LogActivityAsync(string message)
        {
            var admin = await _userManager.GetUserAsync(User);
            await _dashboardService.AddActivityAsync(
                message,
                admin?.Id,
                SD.Role_Admin);
        }
    }

    public class AdminUserListItem
    {
        public ApplicationUser User { get; set; } = null!;
        public List<string> Roles { get; set; } = new();
    }
}
