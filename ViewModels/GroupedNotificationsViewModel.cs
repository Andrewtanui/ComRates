using TanuiApp.Models;

namespace TanuiApp.ViewModels
{
    public class GroupedNotificationsViewModel
    {
        public string DateGroup { get; set; } = string.Empty;
        public List<Notification> Notifications { get; set; } = new List<Notification>();
    }
}
