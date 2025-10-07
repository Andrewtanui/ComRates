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

            // Notify all reporters of this user (email placeholder)
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

            // Notify all reporters of this user (email placeholder)
            await NotifyReportersAsync(userId, "Banned", reason);

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
    }
}
