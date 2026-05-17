using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using SmartClassAI.WebApp.Utility;

namespace SmartClassAI.WebApp.Hubs
{
    [Authorize(Roles = SD.Role_Admin)]
    public class AdminHub : Hub
    {
        public async Task SendDashboardUpdate(object data)
        {
            await Clients.All.SendAsync("ReceiveDashboardUpdate", data);
        }

        public async Task SendActivity(string message)
        {
            await Clients.All.SendAsync("ReceiveActivity", message);
        }
    }
}

