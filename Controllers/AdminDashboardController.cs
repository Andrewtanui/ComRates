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
using TanuiApp.Services;

namespace TanuiApp.Controllers
{
    [Authorize(Roles = "SystemAdmin")]
    public class AdminDashboardController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<Users> _userManager;
        private readonly ILogger<AdminDashboardController> _logger;
        private readonly IEmailSender _emailSender;

        public AdminDashboardController(
            AppDbContext context,
            UserManager<Users> userManager,
            ILogger<AdminDashboardController> logger,
            IEmailSender emailSender)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
            _emailSender = emailSender;
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

            // Suspend all user's products (hide from marketplace)
            var userProducts = await _context.Products.Where(p => p.UserId == userId).ToListAsync();
            foreach (var product in userProducts)
            {
                product.IsActive = false;
            }
            await _context.SaveChangesAsync();

            _logger.LogInformation("Suspended {ProductCount} products for user {UserId}", userProducts.Count, userId);

            // Notify user
            var notification = new Notification
            {
                UserId = userId,
                Title = "Account Suspended",
                Body = $"Your account has been suspended. Reason: {reason}. All your product listings have been hidden.",
                Type = "admin",
                CreatedAt = DateTime.Now
            };
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            // Send email to suspended user
            if (!string.IsNullOrWhiteSpace(user.Email))
            {
                try
                {
                    var emailBody = GenerateSuspendedUserEmailHtml(user.FullName, reason, userProducts.Count);
                    await _emailSender.SendEmailAsync(user.Email, "Account Suspended - Action Required", emailBody);
                    _logger.LogInformation("Sent suspension email to user {UserId} at {Email}", userId, user.Email);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send suspension email to user {UserId}", userId);
                }
            }

            // Notify all reporters of this user
            await NotifyReportersAsync(userId, "Suspended", reason);

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

            // Reactivate all user's products
            var userProducts = await _context.Products.Where(p => p.UserId == userId).ToListAsync();
            foreach (var product in userProducts)
            {
                product.IsActive = true;
            }
            await _context.SaveChangesAsync();

            _logger.LogInformation("Reactivated {ProductCount} products for user {UserId}", userProducts.Count, userId);

            // Notify user
            var notification = new Notification
            {
                UserId = userId,
                Title = "Account Restored",
                Body = "Your account suspension has been lifted. Your product listings are now visible again. Welcome back!",
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

            // Deactivate all user's products permanently
            var userProducts = await _context.Products.Where(p => p.UserId == userId).ToListAsync();
            var productIds = userProducts.Select(p => p.Id).ToList();
            foreach (var product in userProducts)
            {
                product.IsActive = false;
            }
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deactivated {ProductCount} products for banned user {UserId}", userProducts.Count, userId);

            // Process refunds for pending/in-transit orders containing banned user's products
            var affectedOrders = await _context.Orders
                .Include(o => o.Items)
                .ThenInclude(oi => oi.Product)
                .Where(o => o.Items.Any(oi => productIds.Contains(oi.ProductId)) 
                         && (o.Status == "Pending" || o.Status == "Paid")
                         && (o.DeliveryStatus == "Preparing" || o.DeliveryStatus == "Packed" || o.DeliveryStatus == "InTransit"))
                .ToListAsync();

            int refundedOrdersCount = 0;
            foreach (var order in affectedOrders)
            {
                // Mark order as refunded/cancelled
                order.Status = "Refunded";
                order.DeliveryStatus = "Cancelled";

                // Notify buyer about refund
                var buyerNotification = new Notification
                {
                    UserId = order.UserId,
                    Title = "Order Refunded - Seller Account Banned",
                    Body = $"Your order #{order.Id} has been refunded because the seller's account was permanently banned. Amount: ${order.TotalAmount:F2}. The refund will be processed within 3-5 business days.",
                    Type = "admin",
                    CreatedAt = DateTime.Now
                };
                _context.Notifications.Add(buyerNotification);

                refundedOrdersCount++;
                _logger.LogInformation("Refunded order {OrderId} for buyer {BuyerId} due to banned seller {SellerId}", order.Id, order.UserId, userId);
            }

            await _context.SaveChangesAsync();

            // Notify banned user
            var notification = new Notification
            {
                UserId = userId,
                Title = "Account Banned",
                Body = $"Your account has been permanently banned. Reason: {reason}. All your listings have been removed and pending orders have been refunded.",
                Type = "admin",
                CreatedAt = DateTime.Now
            };
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            // Send email to banned user
            if (!string.IsNullOrWhiteSpace(user.Email))
            {
                try
                {
                    var emailBody = GenerateBannedUserEmailHtml(user.FullName, reason, userProducts.Count, refundedOrdersCount);
                    await _emailSender.SendEmailAsync(user.Email, "Account Permanently Banned - Final Notice", emailBody);
                    _logger.LogInformation("Sent ban email to user {UserId} at {Email}", userId, user.Email);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send ban email to user {UserId}", userId);
                }
            }

            // Notify all reporters of this user
            await NotifyReportersAsync(userId, "Banned", reason);

            _logger.LogInformation("Banned user {UserId}. Deactivated {ProductCount} products and refunded {OrderCount} orders", 
                userId, userProducts.Count, refundedOrdersCount);

            return Json(new { success = true, message = $"User banned successfully. {refundedOrdersCount} orders refunded." });
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

            // Notify the reporter of the resolution (email placeholder)
            if (!string.IsNullOrWhiteSpace(report.ReporterId))
            {
                var notify = new Notification
                {
                    UserId = report.ReporterId,
                    Title = "Report Reviewed",
                    Body = $"Your report regarding user {report.ReportedUserId} has been reviewed. Notes: {adminNotes}",
                    Type = "admin",
                    CreatedAt = DateTime.Now
                };
                _context.Notifications.Add(notify);
                await _context.SaveChangesAsync();

                _logger.LogInformation("[EmailPlaceholder] Sent report resolution email to reporter {ReporterId} for report {ReportId}", report.ReporterId, report.Id);
            }

            return Json(new { success = true, message = "Report resolved successfully" });
        }

        // Helper: notify all reporters who reported a given user
        private async Task NotifyReportersAsync(string reportedUserId, string action, string? reasonOrNotes)
        {
            var reporterIds = await _context.UserReports
                .Where(r => r.ReportedUserId == reportedUserId)
                .Select(r => r.ReporterId)
                .Distinct()
                .ToListAsync();

            // Fetch reporters with emails
            var reporters = await _context.Users
                .Where(u => reporterIds.Contains(u.Id) && u.Email != null)
                .Select(u => new { u.Id, u.Email, u.FullName })
                .ToListAsync();

            foreach (var rep in reporters)
            {
                string emailSubject;
                string emailBody;
                string notificationBody;

                if (action.Equals("Suspended", StringComparison.OrdinalIgnoreCase))
                {
                    emailSubject = "Account Suspended - Your Report Has Been Reviewed";
                    emailBody = GenerateSuspendedEmailHtml(rep.FullName, reasonOrNotes ?? "Policy violation");
                    notificationBody = $"The account you reported has been suspended. Reason: {reasonOrNotes}";
                }
                else if (action.Equals("Banned", StringComparison.OrdinalIgnoreCase))
                {
                    emailSubject = "Account Banned - Your Report Has Been Reviewed";
                    emailBody = GenerateBannedEmailHtml(rep.FullName, reasonOrNotes ?? "Severe policy violation");
                    notificationBody = $"The account you reported has been permanently banned. Reason: {reasonOrNotes}";
                }
                else
                {
                    emailSubject = $"Report Update: User {action}";
                    emailBody = $"<p>Thank you for your report. The account you reported has been {action.ToLowerInvariant()}.</p><p>Reason: {System.Net.WebUtility.HtmlEncode(reasonOrNotes)}</p>";
                    notificationBody = $"The account you reported has been {action.ToLowerInvariant()}. Reason: {reasonOrNotes}";
                }

                var n = new Notification
                {
                    UserId = rep.Id,
                    Title = $"Report Update: User {action}",
                    Body = notificationBody,
                    Type = "admin",
                    CreatedAt = DateTime.Now
                };
                _context.Notifications.Add(n);

                // Send email via SMTP
                try
                {
                    await _emailSender.SendEmailAsync(rep.Email!, emailSubject, emailBody);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send reporter email to {Email}", rep.Email);
                }

                _logger.LogInformation("Notified reporter {ReporterId} about {Action} of user {ReportedUserId}", rep.Id, action, reportedUserId);
            }

            await _context.SaveChangesAsync();
        }

        private string GenerateSuspendedEmailHtml(string reporterName, string reason)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f4f7fa; margin: 0; padding: 0; }}
        .container {{ max-width: 600px; margin: 40px auto; background: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 12px rgba(0,0,0,0.1); }}
        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 40px 30px; text-align: center; }}
        .header h1 {{ margin: 0; font-size: 28px; font-weight: 600; }}
        .header p {{ margin: 10px 0 0; font-size: 16px; opacity: 0.95; }}
        .content {{ padding: 40px 30px; color: #333; line-height: 1.8; }}
        .content h2 {{ color: #667eea; font-size: 22px; margin-top: 0; margin-bottom: 20px; }}
        .content p {{ margin: 15px 0; font-size: 15px; }}
        .highlight-box {{ background: #fff3cd; border-left: 4px solid #ffc107; padding: 20px; margin: 25px 0; border-radius: 6px; }}
        .highlight-box strong {{ color: #856404; font-size: 16px; }}
        .highlight-box p {{ margin: 10px 0 0; color: #856404; font-size: 15px; }}
        .info-section {{ background: #f8f9fa; padding: 20px; border-radius: 8px; margin: 25px 0; }}
        .info-section h3 {{ margin: 0 0 15px; color: #495057; font-size: 18px; }}
        .info-section ul {{ margin: 10px 0; padding-left: 20px; }}
        .info-section li {{ margin: 8px 0; color: #6c757d; }}
        .footer {{ background: #f8f9fa; padding: 30px; text-align: center; color: #6c757d; font-size: 13px; border-top: 1px solid #e9ecef; }}
        .footer p {{ margin: 8px 0; }}
        .badge {{ display: inline-block; padding: 8px 16px; background: #28a745; color: white; border-radius: 20px; font-size: 14px; font-weight: 600; margin: 15px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🛡️ Account Action Taken</h1>
            <p>Your report has been reviewed and action has been taken</p>
        </div>
        <div class='content'>
            <h2>Dear {System.Net.WebUtility.HtmlEncode(reporterName)},</h2>
            <p>Thank you for taking the time to report a concerning account on ComRates. We want to inform you that after a thorough investigation by our Trust & Safety team, we have taken decisive action to maintain the integrity and safety of our marketplace community.</p>
            
            <div class='badge'>✓ Account Suspended</div>
            
            <p>The reported account has been <strong>temporarily suspended</strong> from accessing ComRates services. This means the user can no longer list products, make purchases, send messages, or interact with other community members until further review is completed.</p>
            
            <div class='highlight-box'>
                <strong>📋 Suspension Reason:</strong>
                <p>{System.Net.WebUtility.HtmlEncode(reason)}</p>
            </div>
            
            <div class='info-section'>
                <h3>What This Means:</h3>
                <ul>
                    <li><strong>Immediate Effect:</strong> The suspended account is now restricted from all platform activities including buying, selling, and messaging.</li>
                    <li><strong>Review Period:</strong> Our moderation team will conduct a comprehensive review of the account's activity history and determine appropriate next steps.</li>
                    <li><strong>Community Protection:</strong> This suspension helps protect our community from potential harm, fraud, harassment, or policy violations.</li>
                    <li><strong>Your Safety:</strong> You will not receive any further contact from this account while the suspension remains in effect.</li>
                </ul>
            </div>
            
            <div class='info-section'>
                <h3>Your Role in Building a Safe Community:</h3>
                <p>Reports like yours are essential in helping us identify and address problematic behavior on ComRates. By speaking up, you've contributed to making our marketplace safer for everyone. We take every report seriously and investigate each case with care and attention to detail.</p>
                <p>Our commitment is to foster a trustworthy environment where buyers and sellers can transact with confidence, knowing that our team actively monitors and enforces community standards.</p>
            </div>
            
            <p><strong>Need Further Assistance?</strong> If you have additional information about this case or need to report other concerns, please don't hesitate to reach out to our support team. We're here to help ensure your experience on ComRates remains positive and secure.</p>
            
            <p>Thank you for your vigilance and for being a valued member of the ComRates community. Together, we can maintain a marketplace built on trust, respect, and accountability.</p>
            
            <p style='margin-top: 30px;'>Best regards,<br><strong>ComRates Trust & Safety Team</strong></p>
        </div>
        <div class='footer'>
            <p><strong>ComRates Marketplace</strong></p>
            <p>This is an automated notification regarding your report. Please do not reply to this email.</p>
            <p>© 2025 ComRates. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
        }

        private string GenerateBannedEmailHtml(string reporterName, string reason)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f4f7fa; margin: 0; padding: 0; }}
        .container {{ max-width: 600px; margin: 40px auto; background: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 12px rgba(0,0,0,0.1); }}
        .header {{ background: linear-gradient(135deg, #dc3545 0%, #c82333 100%); color: white; padding: 40px 30px; text-align: center; }}
        .header h1 {{ margin: 0; font-size: 28px; font-weight: 600; }}
        .header p {{ margin: 10px 0 0; font-size: 16px; opacity: 0.95; }}
        .content {{ padding: 40px 30px; color: #333; line-height: 1.8; }}
        .content h2 {{ color: #dc3545; font-size: 22px; margin-top: 0; margin-bottom: 20px; }}
        .content p {{ margin: 15px 0; font-size: 15px; }}
        .highlight-box {{ background: #f8d7da; border-left: 4px solid #dc3545; padding: 20px; margin: 25px 0; border-radius: 6px; }}
        .highlight-box strong {{ color: #721c24; font-size: 16px; }}
        .highlight-box p {{ margin: 10px 0 0; color: #721c24; font-size: 15px; }}
        .info-section {{ background: #f8f9fa; padding: 20px; border-radius: 8px; margin: 25px 0; }}
        .info-section h3 {{ margin: 0 0 15px; color: #495057; font-size: 18px; }}
        .info-section ul {{ margin: 10px 0; padding-left: 20px; }}
        .info-section li {{ margin: 8px 0; color: #6c757d; }}
        .footer {{ background: #f8f9fa; padding: 30px; text-align: center; color: #6c757d; font-size: 13px; border-top: 1px solid #e9ecef; }}
        .footer p {{ margin: 8px 0; }}
        .badge {{ display: inline-block; padding: 8px 16px; background: #dc3545; color: white; border-radius: 20px; font-size: 14px; font-weight: 600; margin: 15px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🚫 Permanent Account Ban</h1>
            <p>Decisive action taken following your report</p>
        </div>
        <div class='content'>
            <h2>Dear {System.Net.WebUtility.HtmlEncode(reporterName)},</h2>
            <p>We are writing to inform you of a significant enforcement action taken by the ComRates Trust & Safety team. Following your report and our subsequent comprehensive investigation, we have determined that the reported account engaged in serious violations of our Community Guidelines and Terms of Service.</p>
            
            <div class='badge'>⛔ Permanently Banned</div>
            
            <p>The reported account has been <strong>permanently banned</strong> from ComRates. This is our most severe enforcement action and means the user is prohibited from accessing our platform indefinitely. The associated email address, payment methods, and device identifiers have been blacklisted to prevent circumvention of this ban.</p>
            
            <div class='highlight-box'>
                <strong>📋 Ban Reason:</strong>
                <p>{System.Net.WebUtility.HtmlEncode(reason)}</p>
            </div>
            
            <div class='info-section'>
                <h3>What This Permanent Ban Entails:</h3>
                <ul>
                    <li><strong>Complete Platform Removal:</strong> The banned account has been completely removed from ComRates with no possibility of reinstatement or appeal.</li>
                    <li><strong>All Listings Removed:</strong> Any products or services listed by this account have been immediately delisted and are no longer visible to the community.</li>
                    <li><strong>Transaction History:</strong> All pending transactions involving this account have been cancelled and affected users have been notified.</li>
                    <li><strong>Prevention Measures:</strong> Advanced fraud detection systems have been activated to prevent this individual from creating new accounts.</li>
                    <li><strong>Legal Consideration:</strong> In cases involving fraud, harassment, or illegal activity, relevant information may be shared with law enforcement authorities.</li>
                </ul>
            </div>
            
            <div class='info-section'>
                <h3>Why Permanent Bans Are Necessary:</h3>
                <p>ComRates maintains a zero-tolerance policy for severe violations including fraud, harassment, threats, sale of prohibited items, identity theft, and repeated policy violations. Permanent bans serve to protect our community members from harm and maintain the trust that is fundamental to our marketplace.</p>
                <p>Your report played a crucial role in identifying this problematic account. Without community members like you who are willing to speak up when they witness concerning behavior, it would be significantly more difficult to maintain a safe and trustworthy platform.</p>
            </div>
            
            <div class='info-section'>
                <h3>Your Continued Safety:</h3>
                <p>You will not receive any further contact from this banned account. If you experience any attempts at contact through external channels or notice suspicious new accounts that may be attempting to circumvent this ban, please report them immediately to our Trust & Safety team.</p>
                <p>We also recommend reviewing your account security settings and enabling two-factor authentication if you haven't already done so, as an additional layer of protection for your ComRates account.</p>
            </div>
            
            <p><strong>Thank You for Your Vigilance:</strong> Your willingness to report concerning behavior demonstrates the kind of community responsibility that makes ComRates a safer place for everyone. We deeply appreciate your contribution to maintaining our platform's integrity.</p>
            
            <p>If you have any questions about this enforcement action or need to report additional concerns, our support team is available 24/7 to assist you. Your safety and peace of mind are our top priorities.</p>
            
            <p style='margin-top: 30px;'>With sincere appreciation,<br><strong>ComRates Trust & Safety Team</strong></p>
        </div>
        <div class='footer'>
            <p><strong>ComRates Marketplace</strong></p>
            <p>This is an automated notification regarding your report. Please do not reply to this email.</p>
            <p>For support inquiries, please visit our Help Center or contact support@comrates.com</p>
            <p>© 2025 ComRates. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
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

        // Delivery Companies management
        public async Task<IActionResult> DeliveryCompanies()
        {
            var companies = await _context.DeliveryCompanies
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
            return View(companies);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateDeliveryCompany(string name, string? licenseNumber, string? contactEmail, string? contactPhone, string? town, string? county)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                TempData["Error"] = "Company name is required.";
                return RedirectToAction("DeliveryCompanies");
            }

            _context.DeliveryCompanies.Add(new DeliveryCompany
            {
                Name = name.Trim(),
                LicenseNumber = licenseNumber,
                ContactEmail = contactEmail,
                ContactPhone = contactPhone,
                Town = town,
                County = county,
                IsActive = true,
                CreatedAt = DateTime.Now
            });
            await _context.SaveChangesAsync();

            TempData["Success"] = "Delivery company created.";
            return RedirectToAction("DeliveryCompanies");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleDeliveryCompany(int id)
        {
            var company = await _context.DeliveryCompanies.FindAsync(id);
            if (company == null)
            {
                TempData["Error"] = "Company not found.";
                return RedirectToAction("DeliveryCompanies");
            }
            company.IsActive = !company.IsActive;
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Company {(company.IsActive ? "activated" : "deactivated")}.";
            return RedirectToAction("DeliveryCompanies");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDeliveryCompany(int id)
        {
            var company = await _context.DeliveryCompanies.FindAsync(id);
            if (company == null)
            {
                TempData["Error"] = "Company not found.";
                return RedirectToAction("DeliveryCompanies");
            }

            _context.DeliveryCompanies.Remove(company);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Company deleted.";
            return RedirectToAction("DeliveryCompanies");
        }

        // Email templates for suspended/banned users
        private string GenerateSuspendedUserEmailHtml(string userName, string reason, int productCount)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f4f7fa; margin: 0; padding: 0; }}
        .container {{ max-width: 600px; margin: 40px auto; background: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 12px rgba(0,0,0,0.1); }}
        .header {{ background: linear-gradient(135deg, #ffc107 0%, #ff9800 100%); color: white; padding: 40px 30px; text-align: center; }}
        .header h1 {{ margin: 0; font-size: 28px; font-weight: 600; }}
        .header p {{ margin: 10px 0 0; font-size: 16px; opacity: 0.95; }}
        .content {{ padding: 40px 30px; color: #333; line-height: 1.8; }}
        .content h2 {{ color: #ff9800; font-size: 22px; margin-top: 0; margin-bottom: 20px; }}
        .content p {{ margin: 15px 0; font-size: 15px; }}
        .alert-box {{ background: #fff3cd; border-left: 4px solid #ffc107; padding: 20px; margin: 25px 0; border-radius: 6px; }}
        .alert-box strong {{ color: #856404; font-size: 16px; }}
        .alert-box p {{ margin: 10px 0 0; color: #856404; font-size: 15px; }}
        .info-section {{ background: #f8f9fa; padding: 20px; border-radius: 8px; margin: 25px 0; }}
        .info-section h3 {{ margin: 0 0 15px; color: #495057; font-size: 18px; }}
        .info-section ul {{ margin: 10px 0; padding-left: 20px; }}
        .info-section li {{ margin: 8px 0; color: #6c757d; }}
        .stats {{ background: #fff3cd; padding: 15px; border-radius: 8px; margin: 20px 0; text-align: center; }}
        .stats strong {{ color: #856404; font-size: 18px; }}
        .footer {{ background: #f8f9fa; padding: 30px; text-align: center; color: #6c757d; font-size: 13px; border-top: 1px solid #e9ecef; }}
        .footer p {{ margin: 8px 0; }}
        .badge {{ display: inline-block; padding: 8px 16px; background: #ffc107; color: #333; border-radius: 20px; font-size: 14px; font-weight: 600; margin: 15px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>⚠️ Account Suspended</h1>
            <p>Important notice regarding your ComRates account</p>
        </div>
        <div class='content'>
            <h2>Dear {System.Net.WebUtility.HtmlEncode(userName)},</h2>
            <p>We are writing to inform you that your ComRates account has been temporarily suspended following a review by our Trust & Safety team. This decision was made after careful consideration of reports received and our investigation into your account activity.</p>
            
            <div class='badge'>⏸️ Account Suspended</div>
            
            <div class='alert-box'>
                <strong>📋 Suspension Reason:</strong>
                <p>{System.Net.WebUtility.HtmlEncode(reason)}</p>
            </div>
            
            <div class='stats'>
                <strong>{productCount}</strong> product listing{(productCount != 1 ? "s have" : " has")} been hidden from the marketplace
            </div>
            
            <div class='info-section'>
                <h3>What This Means for Your Account:</h3>
                <ul>
                    <li><strong>Account Access:</strong> You can still log in to your account but with limited functionality. You cannot create new listings, make purchases, or send messages to other users.</li>
                    <li><strong>Product Listings:</strong> All {productCount} of your product listing{(productCount != 1 ? "s are" : " is")} currently hidden from public view and cannot be purchased by buyers.</li>
                    <li><strong>Pending Transactions:</strong> Any ongoing transactions will continue to be processed. Please fulfill any existing orders to maintain your seller reputation.</li>
                    <li><strong>Account Review:</strong> Our moderation team is conducting a comprehensive review of your account. This process typically takes 3-7 business days.</li>
                    <li><strong>Communication:</strong> You will receive updates via email regarding the status of your account review.</li>
                </ul>
            </div>
            
            <div class='info-section'>
                <h3>Next Steps & Appeal Process:</h3>
                <p>If you believe this suspension was made in error or you would like to provide additional context about your account activity, you have the right to appeal this decision. To submit an appeal:</p>
                <ul>
                    <li>Reply to this email with a detailed explanation of your situation</li>
                    <li>Include any relevant evidence or documentation that supports your case</li>
                    <li>Our appeals team will review your submission within 5 business days</li>
                    <li>You will receive a final decision via email</li>
                </ul>
                <p>Please note that submitting an appeal does not guarantee reinstatement. All decisions are made based on our Community Guidelines and Terms of Service.</p>
            </div>
            
            <div class='info-section'>
                <h3>Understanding Our Community Standards:</h3>
                <p>ComRates is committed to maintaining a safe, trustworthy marketplace for all users. Account suspensions are issued when we identify behavior that violates our policies, including but not limited to:</p>
                <ul>
                    <li>Fraudulent or deceptive practices</li>
                    <li>Harassment or abusive behavior toward other users</li>
                    <li>Listing prohibited or counterfeit items</li>
                    <li>Manipulation of reviews or ratings</li>
                    <li>Violation of intellectual property rights</li>
                    <li>Failure to fulfill orders or provide accurate product descriptions</li>
                </ul>
            </div>
            
            <p><strong>Important Reminder:</strong> During the suspension period, please refrain from creating new accounts or attempting to circumvent this suspension. Such actions will result in permanent account termination and may affect your ability to use ComRates in the future.</p>
            
            <p>We understand that account suspensions can be frustrating. Our goal is to ensure a fair and safe marketplace for everyone. If you have questions about this suspension or need clarification on our policies, please don't hesitate to contact our support team.</p>
            
            <p style='margin-top: 30px;'>Sincerely,<br><strong>ComRates Trust & Safety Team</strong></p>
        </div>
        <div class='footer'>
            <p><strong>ComRates Marketplace</strong></p>
            <p>For questions or appeals, reply to this email or contact support@comrates.com</p>
            <p>© 2025 ComRates. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
        }

        private string GenerateBannedUserEmailHtml(string userName, string reason, int productCount, int refundedOrders)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f4f7fa; margin: 0; padding: 0; }}
        .container {{ max-width: 600px; margin: 40px auto; background: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 12px rgba(0,0,0,0.1); }}
        .header {{ background: linear-gradient(135deg, #dc3545 0%, #bd2130 100%); color: white; padding: 40px 30px; text-align: center; }}
        .header h1 {{ margin: 0; font-size: 28px; font-weight: 600; }}
        .header p {{ margin: 10px 0 0; font-size: 16px; opacity: 0.95; }}
        .content {{ padding: 40px 30px; color: #333; line-height: 1.8; }}
        .content h2 {{ color: #dc3545; font-size: 22px; margin-top: 0; margin-bottom: 20px; }}
        .content p {{ margin: 15px 0; font-size: 15px; }}
        .alert-box {{ background: #f8d7da; border-left: 4px solid #dc3545; padding: 20px; margin: 25px 0; border-radius: 6px; }}
        .alert-box strong {{ color: #721c24; font-size: 16px; }}
        .alert-box p {{ margin: 10px 0 0; color: #721c24; font-size: 15px; }}
        .info-section {{ background: #f8f9fa; padding: 20px; border-radius: 8px; margin: 25px 0; }}
        .info-section h3 {{ margin: 0 0 15px; color: #495057; font-size: 18px; }}
        .info-section ul {{ margin: 10px 0; padding-left: 20px; }}
        .info-section li {{ margin: 8px 0; color: #6c757d; }}
        .stats {{ background: #f8d7da; padding: 15px; border-radius: 8px; margin: 20px 0; text-align: center; }}
        .stats strong {{ color: #721c24; font-size: 18px; }}
        .footer {{ background: #f8f9fa; padding: 30px; text-align: center; color: #6c757d; font-size: 13px; border-top: 1px solid #e9ecef; }}
        .footer p {{ margin: 8px 0; }}
        .badge {{ display: inline-block; padding: 8px 16px; background: #dc3545; color: white; border-radius: 20px; font-size: 14px; font-weight: 600; margin: 15px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🚫 Account Permanently Banned</h1>
            <p>Final notice regarding your ComRates account</p>
        </div>
        <div class='content'>
            <h2>Dear {System.Net.WebUtility.HtmlEncode(userName)},</h2>
            <p>This is to inform you that your ComRates account has been <strong>permanently banned</strong> effective immediately. This decision was made by our Trust & Safety team following a thorough investigation into serious violations of our Community Guidelines and Terms of Service.</p>
            
            <div class='badge'>⛔ Permanently Banned</div>
            
            <div class='alert-box'>
                <strong>📋 Ban Reason:</strong>
                <p>{System.Net.WebUtility.HtmlEncode(reason)}</p>
            </div>
            
            <div class='stats'>
                <strong>{productCount}</strong> product{(productCount != 1 ? "s" : "")} removed • <strong>{refundedOrders}</strong> order{(refundedOrders != 1 ? "s" : "")} refunded
            </div>
            
            <div class='info-section'>
                <h3>What This Ban Means:</h3>
                <ul>
                    <li><strong>Account Termination:</strong> Your ComRates account has been permanently closed and cannot be reinstated under any circumstances.</li>
                    <li><strong>Access Revoked:</strong> You are immediately prohibited from accessing the ComRates platform. Any attempt to log in will be denied.</li>
                    <li><strong>All Listings Removed:</strong> Your {productCount} product listing{(productCount != 1 ? "s have" : " has")} been permanently removed from the marketplace and cannot be recovered.</li>
                    <li><strong>Orders Refunded:</strong> {refundedOrders} pending order{(refundedOrders != 1 ? "s have" : " has")} been automatically cancelled and refunded to the buyers. Affected customers have been notified.</li>
                    <li><strong>Email Blacklisted:</strong> The email address associated with this account has been permanently blacklisted and cannot be used to create a new ComRates account.</li>
                    <li><strong>Payment Methods Blocked:</strong> Any payment methods linked to your account have been flagged to prevent circumvention of this ban.</li>
                    <li><strong>No Appeal Available:</strong> This decision is final and not subject to appeal. We will not respond to requests for reinstatement.</li>
                </ul>
            </div>
            
            <div class='info-section'>
                <h3>Why Permanent Bans Are Issued:</h3>
                <p>ComRates maintains a zero-tolerance policy for severe violations that compromise the safety, trust, and integrity of our marketplace community. Permanent bans are reserved for the most serious offenses, including:</p>
                <ul>
                    <li>Fraudulent transactions or scam operations</li>
                    <li>Repeated harassment, threats, or abusive behavior</li>
                    <li>Sale of illegal, dangerous, or prohibited items</li>
                    <li>Identity theft or impersonation</li>
                    <li>Systematic manipulation of the platform (reviews, ratings, pricing)</li>
                    <li>Multiple policy violations despite previous warnings</li>
                    <li>Circumvention of previous suspensions or bans</li>
                </ul>
                <p>These violations not only breach our Terms of Service but also harm other users and undermine the trust that is essential to a functioning marketplace.</p>
            </div>
            
            <div class='info-section'>
                <h3>Legal Considerations:</h3>
                <p>Please be advised that in cases involving fraud, illegal activity, or significant financial harm to other users, ComRates reserves the right to:</p>
                <ul>
                    <li>Report relevant information to law enforcement authorities</li>
                    <li>Cooperate with legal investigations and provide evidence as required by law</li>
                    <li>Pursue civil or criminal action to recover damages or enforce our rights</li>
                    <li>Share information with other platforms to prevent further fraudulent activity</li>
                </ul>
            </div>
            
            <div class='info-section'>
                <h3>Important Warnings:</h3>
                <p><strong>Do Not Attempt to Circumvent This Ban:</strong> Creating new accounts, using different email addresses, or employing any method to bypass this permanent ban will result in immediate termination of those accounts and may lead to legal action. Our fraud detection systems are designed to identify and prevent such attempts.</p>
                <p><strong>Outstanding Financial Obligations:</strong> If you have any outstanding financial obligations to ComRates or to buyers (such as unfulfilled orders or disputed transactions), you remain legally responsible for resolving these matters despite the account ban.</p>
            </div>
            
            <p><strong>Final Statement:</strong> We do not take the decision to permanently ban accounts lightly. This action was necessary to protect our community and maintain the integrity of the ComRates marketplace. We wish you well in your future endeavors, but you are no longer welcome on our platform.</p>
            
            <p>If you have urgent questions regarding outstanding financial matters or legal obligations related to this ban, you may contact our legal department at legal@comrates.com. Please note that we will not respond to appeals or requests for account reinstatement.</p>
            
            <p style='margin-top: 30px;'>ComRates Trust & Safety Team</p>
        </div>
        <div class='footer'>
            <p><strong>ComRates Marketplace</strong></p>
            <p>This is a final notice. Your account has been permanently terminated.</p>
            <p>© 2025 ComRates. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
        }
    }
}
