using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace TanuiApp.Hubs
{
    [Authorize]
    public class MessageHub : Hub
    {
        public async Task JoinThread(string threadKey)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, threadKey);
        }

        public async Task LeaveThread(string threadKey)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, threadKey);
        }
    }
}


