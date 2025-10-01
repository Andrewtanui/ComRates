using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TanuiApp.Data;
using TanuiApp.Models;
using TanuiApp.ViewModels;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace TanuiApp.Controllers
{
    [Authorize(Roles = "DeliveryService,SystemAdmin")]
    public class DeliveryDashboardController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<Users> _userManager;
        private readonly ILogger<DeliveryDashboardController> _logger;

        public DeliveryDashboardController(
            AppDbContext context,
            UserManager<Users> userManager,
            ILogger<DeliveryDashboardController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var viewModel = new DeliveryDashboardViewModel();

            // Get all deliveries assigned to this delivery service
            var myDeliveries = await _context.Orders
                .Include(o => o.Buyer)
                .Include(o => o.Items)
                .Where(o => o.DeliveryServiceId == userId)
                .ToListAsync();

            viewModel.TotalDeliveries = myDeliveries.Count;
            viewModel.PendingDeliveries = myDeliveries.Count(o => o.DeliveryStatus == "Preparing" || o.DeliveryStatus == "Packed");
            viewModel.InTransitDeliveries = myDeliveries.Count(o => o.DeliveryStatus == "InTransit");
            viewModel.CompletedDeliveries = myDeliveries.Count(o => o.DeliveryStatus == "Delivered");
            viewModel.TotalEarnings = myDeliveries.Sum(o => o.DeliveryFee);
            viewModel.AverageDeliveryFee = viewModel.TotalDeliveries > 0 
                ? viewModel.TotalEarnings / viewModel.TotalDeliveries 
                : 0;

            // Pending orders (Preparing or Packed)
            viewModel.PendingOrders = myDeliveries
                .Where(o => o.DeliveryStatus == "Preparing" || o.DeliveryStatus == "Packed")
                .OrderBy(o => o.OrderDate)
                .Select(o => new DeliveryOrderInfo
                {
                    OrderId = o.Id,
                    TrackingNumber = o.TrackingNumber ?? $"TRK{o.Id:D6}",
                    OrderDate = o.OrderDate,
                    CustomerName = o.Buyer?.FullName ?? o.UserId,
                    DeliveryAddress = o.DeliveryAddress ?? "Not specified",
                    DeliveryTown = o.DeliveryTown ?? "",
                    DeliveryCounty = o.DeliveryCounty ?? "",
                    DeliveryStatus = o.DeliveryStatus,
                    DeliveryFee = o.DeliveryFee,
                    TotalAmount = o.TotalAmount,
                    ItemCount = o.Items.Count,
                    DeliveryDate = o.DeliveryDate,
                    Latitude = o.BuyerLatitude,
                    Longitude = o.BuyerLongitude
                })
                .ToList();

            // Active deliveries (In Transit)
            viewModel.ActiveDeliveries = myDeliveries
                .Where(o => o.DeliveryStatus == "InTransit")
                .OrderBy(o => o.ShippedAt)
                .Select(o => new DeliveryOrderInfo
                {
                    OrderId = o.Id,
                    TrackingNumber = o.TrackingNumber ?? $"TRK{o.Id:D6}",
                    OrderDate = o.OrderDate,
                    CustomerName = o.Buyer?.FullName ?? o.UserId,
                    DeliveryAddress = o.DeliveryAddress ?? "Not specified",
                    DeliveryTown = o.DeliveryTown ?? "",
                    DeliveryCounty = o.DeliveryCounty ?? "",
                    DeliveryStatus = o.DeliveryStatus,
                    DeliveryFee = o.DeliveryFee,
                    TotalAmount = o.TotalAmount,
                    ItemCount = o.Items.Count,
                    DeliveryDate = o.DeliveryDate,
                    Latitude = o.BuyerLatitude,
                    Longitude = o.BuyerLongitude
                })
                .ToList();

            // Recent deliveries (Delivered)
            viewModel.RecentDeliveries = myDeliveries
                .Where(o => o.DeliveryStatus == "Delivered")
                .OrderByDescending(o => o.DeliveredAt)
                .Take(10)
                .Select(o => new DeliveryOrderInfo
                {
                    OrderId = o.Id,
                    TrackingNumber = o.TrackingNumber ?? $"TRK{o.Id:D6}",
                    OrderDate = o.OrderDate,
                    CustomerName = o.Buyer?.FullName ?? o.UserId,
                    DeliveryAddress = o.DeliveryAddress ?? "Not specified",
                    DeliveryTown = o.DeliveryTown ?? "",
                    DeliveryCounty = o.DeliveryCounty ?? "",
                    DeliveryStatus = o.DeliveryStatus,
                    DeliveryFee = o.DeliveryFee,
                    TotalAmount = o.TotalAmount,
                    ItemCount = o.Items.Count,
                    DeliveryDate = o.DeliveryDate,
                    Latitude = o.BuyerLatitude,
                    Longitude = o.BuyerLongitude
                })
                .ToList();

            // Frequent delivery locations
            viewModel.FrequentLocations = myDeliveries
                .Where(o => !string.IsNullOrEmpty(o.DeliveryTown))
                .GroupBy(o => o.DeliveryTown)
                .Select(g => new LocationStats
                {
                    Location = g.Key ?? "Unknown",
                    DeliveryCount = g.Count(),
                    TotalEarnings = g.Sum(o => o.DeliveryFee)
                })
                .OrderByDescending(l => l.DeliveryCount)
                .Take(10)
                .ToList();

            // Deliveries by day of week
            viewModel.DeliveriesByDay = myDeliveries
                .GroupBy(o => o.OrderDate.DayOfWeek)
                .Select(g => new DailyDeliveryStats
                {
                    DayName = g.Key.ToString(),
                    DeliveryCount = g.Count(),
                    Earnings = g.Sum(o => o.DeliveryFee)
                })
                .OrderBy(d => (int)Enum.Parse<DayOfWeek>(d.DayName))
                .ToList();

            // Deliveries by hour
            viewModel.DeliveriesByHour = myDeliveries
                .GroupBy(o => o.OrderDate.Hour)
                .Select(g => new HourlyDeliveryStats
                {
                    Hour = g.Key,
                    TimeRange = $"{g.Key:D2}:00",
                    DeliveryCount = g.Count(),
                    Earnings = g.Sum(o => o.DeliveryFee)
                })
                .OrderBy(h => h.Hour)
                .ToList();

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateDeliveryStatus(int orderId, string status)
        {
            var userId = _userManager.GetUserId(User);
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.Id == orderId && o.DeliveryServiceId == userId);

            if (order == null)
            {
                return Json(new { success = false, message = "Order not found" });
            }

            order.DeliveryStatus = status;

            switch (status)
            {
                case "Packed":
                    order.PackedAt = DateTime.Now;
                    break;
                case "InTransit":
                    order.ShippedAt = DateTime.Now;
                    break;
                case "Delivered":
                    order.DeliveredAt = DateTime.Now;
                    break;
            }

            await _context.SaveChangesAsync();

            // Notify buyer
            var notification = new Notification
            {
                UserId = order.UserId,
                Title = $"Order #{order.Id} - {status}",
                Body = $"Your order is now {status}. Track your delivery!",
                Type = "delivery",
                CreatedAt = DateTime.Now
            };
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Order status updated to {status}";
            return Json(new { success = true, message = $"Status updated to {status}" });
        }
    }
}
