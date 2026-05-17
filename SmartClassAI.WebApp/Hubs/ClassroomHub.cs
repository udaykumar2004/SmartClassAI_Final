using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using SmartClassAI.WebApp.Services;
using System.Security.Claims;

namespace SmartClassAI.WebApp.Hubs
{
    [Authorize]
    public class ClassroomHub : Hub
    {
        private readonly ClassroomAccessService _access;

        public ClassroomHub(ClassroomAccessService access)
        {
            _access = access;
        }

        public async Task JoinClassroom(int classroomId)
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
            {
                Context.Abort();
                return;
            }

            if (!await _access.CanAccessClassroomAsync(user, classroomId))
            {
                Context.Abort();
                return;
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, classroomId.ToString());
        }

        public async Task SendMessage(int classroomId, string userName, string message)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return;

            if (!await _access.CanAccessClassroomAsync(user, classroomId))
                return;

            message = (message ?? string.Empty).Trim();
            if (message.Length == 0 || message.Length > 2000)
                return;

            userName = string.IsNullOrWhiteSpace(userName)
                ? (user.FullName.Length > 0 ? user.FullName : user.Email ?? "User")
                : userName;

            await Clients.Group(classroomId.ToString())
                .SendAsync(
                    "ReceiveMessage",
                    userName,
                    message,
                    DateTime.UtcNow.ToString("hh:mm tt"));
        }

        public async Task NotifyMeetingStarted(int classroomId, string meetingRoomId)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return;

            if (!await _access.CanManageClassroomAsync(user, classroomId))
                return;

            await Clients.Group(classroomId.ToString())
                .SendAsync("MeetingStarted", meetingRoomId);
        }

        public async Task NotifyMeetingEnded(int classroomId)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return;

            if (!await _access.CanManageClassroomAsync(user, classroomId))
                return;

            await Clients.Group(classroomId.ToString())
                .SendAsync("MeetingEnded");
        }

        private async Task<SmartClassAI.Models.ApplicationUser?> GetCurrentUserAsync()
        {
            var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return null;

            var userManager = Context.GetHttpContext()!
                .RequestServices
                .GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<SmartClassAI.Models.ApplicationUser>>();

            return await userManager.FindByIdAsync(userId);
        }
    }
}
