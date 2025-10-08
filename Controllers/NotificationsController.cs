using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TanuiApp.Data;
using TanuiApp.Models;
using TanuiApp.ViewModels;

namespace TanuiApp.Controllers
{
    [Authorize]
    public class NotificationsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<Users> _userManager;

        public NotificationsController(AppDbContext context, UserManager<Users> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(string category = "all")
        {
            var userId = _userManager.GetUserId(User);
            var query = _context.Notifications
                .Where(n => n.UserId == userId);

            // Filter by category if specified
            if (!string.IsNullOrEmpty(category) && category.ToLower() != "all")
            {
                query = query.Where(n => n.Type.ToLower() == category.ToLower());
            }

            var items = await query
                .OrderByDescending(n => n.CreatedAt)
                .Take(100)
                .ToListAsync();

            // Group notifications by date
            var groupedNotifications = GroupNotificationsByDate(items);

            ViewBag.CurrentCategory = category;
            ViewBag.UnreadCounts = await GetUnreadCountsByCategory(userId);
            
            return View(groupedNotifications);
        }

        private List<GroupedNotificationsViewModel> GroupNotificationsByDate(List<Notification> notifications)
        {
            var today = DateTime.Today;
            var yesterday = today.AddDays(-1);
            var grouped = new List<GroupedNotificationsViewModel>();

            // Group by date
            var notificationsByDate = notifications.GroupBy(n => n.CreatedAt.Date);

            foreach (var group in notificationsByDate.OrderByDescending(g => g.Key))
            {
                string dateLabel;
                if (group.Key == today)
                {
                    dateLabel = "Today";
                }
                else if (group.Key == yesterday)
                {
                    dateLabel = "Yesterday";
                }
                else
                {
                    dateLabel = group.Key.ToString("MMMM dd, yyyy");
                }

                grouped.Add(new GroupedNotificationsViewModel
                {
                    DateGroup = dateLabel,
                    Notifications = group.OrderByDescending(n => n.CreatedAt).ToList()
                });
            }

            return grouped;
        }

        private async Task<Dictionary<string, int>> GetUnreadCountsByCategory(string userId)
        {
            var counts = new Dictionary<string, int>
            {
                ["all"] = await _context.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead),
                ["message"] = await _context.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead && n.Type.ToLower() == "message"),
                ["order"] = await _context.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead && n.Type.ToLower() == "order"),
                ["delivery"] = await _context.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead && n.Type.ToLower() == "delivery"),
                ["admin"] = await _context.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead && n.Type.ToLower() == "admin"),
                ["report"] = await _context.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead && n.Type.ToLower() == "report")
            };
            return counts;
        }

        [HttpPost]
        public async Task<IActionResult> MarkAllRead()
        {
            var userId = _userManager.GetUserId(User);
            var items = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();
            foreach (var n in items) n.IsRead = true;
            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = _userManager.GetUserId(User);
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);
            
            if (notification == null)
            {
                return Json(new { success = false, message = "Notification not found" });
            }

            _context.Notifications.Remove(notification);
            await _context.SaveChangesAsync();
            
            return Json(new { success = true, message = "Notification deleted" });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteAll(string category = "all")
        {
            var userId = _userManager.GetUserId(User);
            var query = _context.Notifications.Where(n => n.UserId == userId);

            if (!string.IsNullOrEmpty(category) && category.ToLower() != "all")
            {
                query = query.Where(n => n.Type.ToLower() == category.ToLower());
            }

            var notifications = await query.ToListAsync();
            _context.Notifications.RemoveRange(notifications);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = $"Deleted {notifications.Count} notifications" });
        }
    }
}







