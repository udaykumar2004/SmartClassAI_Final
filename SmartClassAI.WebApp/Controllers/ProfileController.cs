using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SmartClassAI.Models;
using SmartClassAI.WebApp.Services;
using SmartClassAI.WebApp.Utility;

namespace SmartClassAI.WebApp.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly FileUploadService _files;

        public ProfileController(
            UserManager<ApplicationUser> userManager,
            FileUploadService files)
        {
            _userManager = userManager;
            _files = files;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            ViewBag.Roles = await _userManager.GetRolesAsync(user);
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(string fullName, IFormFile? profileImage)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            if (!string.IsNullOrWhiteSpace(fullName))
                user.FullName = fullName.Trim();

            if (profileImage != null)
            {
                var (url, error) = await _files.UploadAsync(
                    profileImage,
                    SD.ProfilePic_Folder,
                    SD.Allowed_Image_Extensions,
                    2 * 1024 * 1024);

                if (error != null)
                {
                    TempData["error"] = error;
                    return View(user);
                }

                _files.DeleteIfExists(user.ProfilePictureUrl);
                user.ProfilePictureUrl = url;
            }

            await _userManager.UpdateAsync(user);
            TempData["success"] = "Profile updated.";
            return RedirectToAction(nameof(Index));
        }
    }
}
