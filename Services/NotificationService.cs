using Microsoft.AspNetCore.SignalR;
using TanuiApp.Data;
using TanuiApp.Hubs;
using TanuiApp.Models;

namespace TanuiApp.Services
{
    public class NotificationService : INotificationService
    {
        private readonly AppDbContext _context;
        private readonly IHubContext<NotificationHub> _hubContext;

        public NotificationService(AppDbContext context, IHubContext<NotificationHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        public async Task SendNotificationAsync(string userId, string title, string body, string type, string? link = null)
        {
            // Save to database
            var notification = new Notification
            {
                UserId = userId,
                Title = title,
                Body = body,
                Type = type,
                Link = link,
                CreatedAt = DateTime.Now,
                IsRead = false
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            // Send real-time notification via SignalR
            try
            {
                await _hubContext.Clients.Group($"user:{userId}").SendAsync("Notify", title, body);
            }
            catch (Exception ex)
            {
                // Log but don't fail if SignalR fails
                Console.WriteLine($"Failed to send SignalR notification: {ex.Message}");
            }
        }
    }
}
