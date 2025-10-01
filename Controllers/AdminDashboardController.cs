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
    [Authorize(Roles = "SystemAdmin")]
    public class AdminDashboardController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<Users> _userManager;
        private readonly ILogger<AdminDashboardController> _logger;

        public AdminDashboardController(
            AppDbContext context,
            UserManager<Users> userManager,
            ILogger<AdminDashboardController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var viewModel = new AdminDashboardViewModel();

            // User statistics
            var allUsers = await _context.Users.ToListAsync();
            viewModel.TotalUsers = allUsers.Count;
            viewModel.TotalBuyers = allUsers.Count(u => u.UserRole == UserRole.Buyer);
            viewModel.TotalSellers = allUsers.Count(u => u.UserRole == UserRole.Seller);
            viewModel.TotalDeliveryServices = allUsers.Count(u => u.UserRole == UserRole.DeliveryService);
            viewModel.SuspendedAccounts = allUsers.Count(u => u.IsSuspended);
            viewModel.BannedAccounts = allUsers.Count(u => u.IsBanned);

            // System statistics
            viewModel.TotalProducts = await _context.Products.CountAsync();
            viewModel.TotalOrders = await _context.Orders.CountAsync();
            viewModel.TotalRevenue = await _context.Orders.SumAsync(o => o.TotalAmount);
            viewModel.ActiveDeliveries = await _context.Orders.CountAsync(o => o.DeliveryStatus == "InTransit");

            // Reports
            var pendingReports = await _context.UserReports
                .Where(r => !r.IsResolved)
                .Include(r => r.ReportedUser)
                .Include(r => r.Reporter)
                .ToListAsync();

            viewModel.PendingReports = pendingReports.Count;
            viewModel.RecentReports = pendingReports
                .OrderByDescending(r => r.ReportedAt)
                .Take(10)
                .Select(r => new UserReportInfo
                {
                    ReportId = r.Id,
                    ReportedUserName = r.ReportedUser?.FullName ?? "Unknown",
                    ReportedUserId = r.ReportedUserId,
                    ReporterName = r.Reporter?.FullName ?? "Anonymous",
                    Reason = r.Reason,
                    Description = r.Description,
                    ReportedAt = r.ReportedAt,
                    IsResolved = r.IsResolved
                })
                .ToList();

            // Recent users
            viewModel.RecentUsers = allUsers
                .OrderByDescending(u => u.CreatedAt)
                .Take(10)
                .Select(u => new UserInfo
                {
                    UserId = u.Id,
                    FullName = u.FullName,
                    Email = u.Email ?? "",
                    UserRole = u.UserRole.ToString(),
                    CreatedAt = u.CreatedAt,
                    IsSuspended = u.IsSuspended,
                    IsBanned = u.IsBanned,
                    ReportCount = u.ReportCount,
                    ProfilePictureUrl = u.ProfilePictureUrl
                })
                .ToList();

            // Flagged users (5+ reports)
            viewModel.FlaggedUsers = allUsers
                .Where(u => u.ReportCount >= 5 && !u.IsBanned)
                .OrderByDescending(u => u.ReportCount)
                .Select(u => new UserInfo
                {
                    UserId = u.Id,
                    FullName = u.FullName,
                    Email = u.Email ?? "",
                    UserRole = u.UserRole.ToString(),
                    CreatedAt = u.CreatedAt,
                    IsSuspended = u.IsSuspended,
                    IsBanned = u.IsBanned,
                    ReportCount = u.ReportCount,
                    ProfilePictureUrl = u.ProfilePictureUrl
                })
                .ToList();

            // Pending delivery services (not verified)
            viewModel.PendingDeliveryServices = allUsers
                .Where(u => u.UserRole == UserRole.DeliveryService && !u.IsVerified)
                .Select(u => new DeliveryServiceInfo
                {
                    UserId = u.Id,
                    FullName = u.FullName,
                    Email = u.Email ?? "",
                    CompanyName = u.CompanyName,
                    LicenseNumber = u.LicenseNumber,
                    VehicleInfo = u.VehicleInfo,
                    Town = u.Town,
                    County = u.County,
                    IsVerified = u.IsVerified,
                    CreatedAt = u.CreatedAt
                })
                .ToList();

            // User growth (last 7 days)
            var sevenDaysAgo = DateTime.Now.AddDays(-7);
            viewModel.UserGrowth = allUsers
                .Where(u => u.CreatedAt >= sevenDaysAgo)
                .GroupBy(u => u.CreatedAt.Date)
                .Select(g => new DailyStats
                {
                    Date = g.Key,
                    Count = g.Count()
                })
                .OrderBy(d => d.Date)
                .ToList();

            // Orders by day
            var orders = await _context.Orders.ToListAsync();
            viewModel.OrdersByDay = orders
                .GroupBy(o => o.OrderDate.DayOfWeek)
                .Select(g => new OrderStats
                {
                    DayName = g.Key.ToString(),
                    OrderCount = g.Count(),
                    Revenue = g.Sum(o => o.TotalAmount)
                })
                .OrderBy(d => (int)Enum.Parse<DayOfWeek>(d.DayName))
                .ToList();

            return View(viewModel);
        }

        // User Management Actions
        [HttpPost]
        public async Task<IActionResult> SuspendUser(string userId, string reason)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return Json(new { success = false, message = "User not found" });
            }

            user.IsSuspended = true;
            user.SuspendedAt = DateTime.Now;
            user.SuspensionReason = reason;

            await _userManager.UpdateAsync(user);

            // Notify user
            var notification = new Notification
            {
                UserId = userId,
                Title = "Account Suspended",
                Body = $"Your account has been suspended. Reason: {reason}",
                Type = "admin",
                CreatedAt = DateTime.Now
            };
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "User suspended successfully" });
        }

        [HttpPost]
        public async Task<IActionResult> UnsuspendUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return Json(new { success = false, message = "User not found" });
            }

            user.IsSuspended = false;
            user.SuspendedAt = null;
            user.SuspensionReason = null;

            await _userManager.UpdateAsync(user);

            // Notify user
            var notification = new Notification
            {
                UserId = userId,
                Title = "Account Restored",
                Body = "Your account suspension has been lifted. Welcome back!",
                Type = "admin",
                CreatedAt = DateTime.Now
            };
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "User unsuspended successfully" });
        }

        [HttpPost]
        public async Task<IActionResult> BanUser(string userId, string reason)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return Json(new { success = false, message = "User not found" });
            }

            user.IsBanned = true;
            user.BannedAt = DateTime.Now;
            user.BanReason = reason;
            user.IsSuspended = true; // Also suspend

            await _userManager.UpdateAsync(user);

            // Notify user
            var notification = new Notification
            {
                UserId = userId,
                Title = "Account Banned",
                Body = $"Your account has been permanently banned. Reason: {reason}. This email can no longer be used.",
                Type = "admin",
                CreatedAt = DateTime.Now
            };
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "User banned successfully" });
        }

        [HttpPost]
        public async Task<IActionResult> VerifyDeliveryService(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || user.UserRole != UserRole.DeliveryService)
            {
                return Json(new { success = false, message = "Delivery service not found" });
            }

            user.IsVerified = true;
            await _userManager.UpdateAsync(user);

            // Notify user
            var notification = new Notification
            {
                UserId = userId,
                Title = "Delivery Service Verified",
                Body = "Congratulations! Your delivery service has been verified. You can now accept deliveries.",
                Type = "admin",
                CreatedAt = DateTime.Now
            };
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Delivery service verified successfully" });
        }

        [HttpPost]
        public async Task<IActionResult> ResolveReport(int reportId, string adminNotes)
        {
            var report = await _context.UserReports.FindAsync(reportId);
            if (report == null)
            {
                return Json(new { success = false, message = "Report not found" });
            }

            report.IsResolved = true;
            report.ResolvedAt = DateTime.Now;
            report.AdminNotes = adminNotes;

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Report resolved successfully" });
        }

        // Views
        public async Task<IActionResult> Users(string role = "all", string status = "all")
        {
            var query = _context.Users.AsQueryable();

            if (role != "all")
            {
                if (Enum.TryParse<UserRole>(role, true, out var userRole))
                {
                    query = query.Where(u => u.UserRole == userRole);
                }
            }

            if (status == "suspended")
            {
                query = query.Where(u => u.IsSuspended);
            }
            else if (status == "banned")
            {
                query = query.Where(u => u.IsBanned);
            }
            else if (status == "flagged")
            {
                query = query.Where(u => u.ReportCount >= 5);
            }

            var users = await query.OrderByDescending(u => u.CreatedAt).ToListAsync();
            ViewBag.Role = role;
            ViewBag.Status = status;

            return View(users);
        }

        public async Task<IActionResult> Reports()
        {
            var reports = await _context.UserReports
                .Include(r => r.ReportedUser)
                .Include(r => r.Reporter)
                .OrderByDescending(r => r.ReportedAt)
                .ToListAsync();

            return View(reports);
        }

        public async Task<IActionResult> DeliveryServices()
        {
            var deliveryServices = await _context.Users
                .Where(u => u.UserRole == UserRole.DeliveryService)
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();

            return View(deliveryServices);
        }
    }
}
