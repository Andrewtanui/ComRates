using TanuiApp.Models;

namespace TanuiApp.Services
{
    public interface INotificationService
    {
        Task SendNotificationAsync(string userId, string title, string body, string type, string? link = null);
    }
}
