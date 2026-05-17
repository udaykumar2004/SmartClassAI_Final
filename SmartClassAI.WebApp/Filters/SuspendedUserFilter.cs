using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SmartClassAI.Models;

namespace SmartClassAI.WebApp.Filters
{
    public class SuspendedUserFilter : IAsyncActionFilter
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public SuspendedUserFilter(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public async Task OnActionExecutionAsync(
            ActionExecutingContext context,
            ActionExecutionDelegate next)
        {
            if (context.HttpContext.User.Identity?.IsAuthenticated == true)
            {
                var user = await _userManager.GetUserAsync(context.HttpContext.User);
                if (user != null && (!user.IsActive || user.IsSuspended))
                {
                    await _signInManager.SignOutAsync();
                    context.Result = new RedirectToActionResult(
                        "Index",
                        "Home",
                        new { area = "" });
                    return;
                }
            }

            await next();
        }
    }
}
