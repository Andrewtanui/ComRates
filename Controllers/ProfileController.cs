using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TanuiApp.Data;
using TanuiApp.Models;
using TanuiApp.ViewModels;
using TanuiApp.Services;

namespace TanuiApp.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly UserManager<Users> _userManager;
        private readonly SignInManager<Users> _signInManager;
        private readonly AppDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<ProfileController> _logger;

        public ProfileController(UserManager<Users> userManager, SignInManager<Users> signInManager, AppDbContext context, INotificationService notificationService, IEmailSender emailSender, ILogger<ProfileController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _notificationService = notificationService;
            _emailSender = emailSender;
            _logger = logger;
        }

        // Display profile settings page
        public async Task<IActionResult> Settings()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var model = new ProfileViewModel
            {
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                DateOfBirth = user.DateOfBirth,
                Address = user.Address,
                Estate = user.Estate,
                Town = user.Town,
                County = user.County,
                PostalCode = user.PostalCode,
                Bio = user.Bio,
                ProfilePictureUrl = user.ProfilePictureUrl,
                EmailNotifications = user.EmailNotifications,
                SmsNotifications = user.SmsNotifications,
                IsPublicProfile = user.IsPublicProfile
            };

            return View(model);
        }

        // Update profile information
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Settings(ProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            // Update user properties
            user.FullName = model.FullName;
            user.PhoneNumber = model.PhoneNumber;
            user.DateOfBirth = model.DateOfBirth;
            user.Address = model.Address;
            user.Estate = model.Estate;
            user.Town = model.Town;
            user.County = model.County;
            user.PostalCode = model.PostalCode;
            user.Bio = model.Bio;
            user.ProfilePictureUrl = model.ProfilePictureUrl;
            user.EmailNotifications = model.EmailNotifications;
            user.SmsNotifications = model.SmsNotifications;
            user.IsPublicProfile = model.IsPublicProfile;

            // Update email if changed
            if (user.Email != model.Email)
            {
                var setEmailResult = await _userManager.SetEmailAsync(user, model.Email);
                if (!setEmailResult.Succeeded)
                {
                    foreach (var error in setEmailResult.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                    }
                    return View(model);
                }

                var setUserNameResult = await _userManager.SetUserNameAsync(user, model.Email);
                if (!setUserNameResult.Succeeded)
                {
                    foreach (var error in setUserNameResult.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                    }
                    return View(model);
                }
            }

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                // Refresh sign in to update claims
                await _signInManager.RefreshSignInAsync(user);
                TempData["Success"] = "Your profile has been updated successfully!";
                return RedirectToAction("Settings");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            return View(model);
        }

        // Update password page (different from reset password)
        public async Task<IActionResult> UpdatePassword()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var model = new ChangePasswordViewModel
            {
                Email = user.Email
            };

            return View(model);
        }

        // Update password
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
            if (result.Succeeded)
            {
                await _signInManager.RefreshSignInAsync(user);
                TempData["Success"] = "Your password has been changed successfully!";
                return RedirectToAction("Settings");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            return View(model);
        }

        // Delete account confirmation page
        public IActionResult DeleteAccount()
        {
            return View();
        }

        // Delete account
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAccountConfirmed()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            // Delete user's cart items
            var cartItems = _context.CartItems.Where(c => c.UserId == user.Id);
            _context.CartItems.RemoveRange(cartItems);

            // Delete user's wishlist items
            var wishlistItems = _context.WishlistItems.Where(w => w.UserId == user.Id);
            _context.WishlistItems.RemoveRange(wishlistItems);

            // Delete user's products
            var products = _context.Products.Where(p => p.UserId == user.Id);
            _context.Products.RemoveRange(products);

            await _context.SaveChangesAsync();

            // Delete the user account
            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                await _signInManager.SignOutAsync();
                TempData["Success"] = "Your account has been deleted successfully.";
                return RedirectToAction("Index", "Home");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            return View("DeleteAccount");
        }

        // Profile statistics
        public async Task<IActionResult> Statistics()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var stats = new
            {
                TotalProducts = await _context.Products.CountAsync(p => p.UserId == user.Id),
                CartItemsCount = await _context.CartItems.CountAsync(c => c.UserId == user.Id),
                WishlistItemsCount = await _context.WishlistItems.CountAsync(w => w.UserId == user.Id),
                AccountAge = (DateTime.Now - user.CreatedAt).Days,
                LastLogin = user.LastLoginAt ?? "Never"
            };

            ViewBag.Stats = stats;
            return View();
        }

        // My Listings (dashboard view of current user's products)
        public async Task<IActionResult> MyListings()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var myProducts = await _context.Products
                .Where(p => p.UserId == user.Id)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            ViewBag.Total = myProducts.Count;
            return View(myProducts);
        }

        // Public catalog of users (only public profiles)
        [AllowAnonymous]
        public async Task<IActionResult> PublicCatalog()
        {
            var users = await _context.Users
                .Where(u => u.IsPublicProfile && u.UserRole != UserRole.SystemAdmin)
                .Select(u => new {
                    u.Id,
                    u.FullName,
                    u.ProfilePictureUrl,
                    Products = _context.Products.Count(p => p.UserId == u.Id)
                })
                .OrderByDescending(u => u.Products)
                .ToListAsync();

            return View(users);
        }

        // Public profile view showing a user's catalog of products
        [AllowAnonymous]
        public async Task<IActionResult> ViewProfile(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return RedirectToAction("PublicCatalog");

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null || !user.IsPublicProfile || user.UserRole == UserRole.SystemAdmin)
            {
                return NotFound();
            }

            var products = await _context.Products
                .Where(p => p.UserId == id)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            ViewBag.User = user;
            return View(products);
        }

        // Report a user account from the public profile page
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReportUser(string id, string reason, string? description)
        {
            // id is the reported user's id
            if (string.IsNullOrWhiteSpace(id))
            {
                TempData["Error"] = "Invalid user to report.";
                return RedirectToAction(nameof(PublicCatalog));
            }

            var reportedUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (reportedUser == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction(nameof(PublicCatalog));
            }

            var reporter = await _userManager.GetUserAsync(User);
            if (reporter == null)
            {
                return Challenge();
            }

            if (reporter.Id == reportedUser.Id)
            {
                TempData["Error"] = "You cannot report yourself.";
                return RedirectToAction(nameof(ViewProfile), new { id });
            }

            if (string.IsNullOrWhiteSpace(reason))
            {
                TempData["Error"] = "Please select a reason for reporting.";
                return RedirectToAction(nameof(ViewProfile), new { id });
            }

            var report = new UserReport
            {
                ReportedUserId = reportedUser.Id,
                ReporterId = reporter.Id,
                Reason = reason,
                Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
                ReportedAt = DateTime.Now,
                IsResolved = false
            };

            _context.UserReports.Add(report);
            await _context.SaveChangesAsync();

            // Notify the reported user
            await _notificationService.SendNotificationAsync(
                reportedUser.Id,
                "You Have Been Reported",
                $"Your account has been reported for: {reason}. Our team will review this matter. Please ensure you follow our community guidelines.",
                "admin"
            );

            // Send email to reported user
            if (!string.IsNullOrWhiteSpace(reportedUser.Email))
            {
                try
                {
                    var emailSubject = "Account Report Notification";
                    var emailBody = $@"
                        <html>
                        <body style='font-family: Arial, sans-serif;'>
                            <h2>Account Report Notification</h2>
                            <p>Dear {reportedUser.FullName},</p>
                            <p>We wanted to inform you that your account has been reported by another user.</p>
                            <p><strong>Reason:</strong> {reason}</p>
                            {(!string.IsNullOrWhiteSpace(description) ? $"<p><strong>Details:</strong> {System.Net.WebUtility.HtmlEncode(description)}</p>" : "")}
                            <p>Our moderation team will review this report. If any violations of our community guidelines are found, appropriate action may be taken.</p>
                            <p>If you believe this report was made in error, please contact our support team.</p>
                            <p>Best regards,<br>The TanuiApp Team</p>
                        </body>
                        </html>";
                    await _emailSender.SendEmailAsync(reportedUser.Email, emailSubject, emailBody);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send report notification email to user {UserId}", reportedUser.Id);
                }
            }

            TempData["Success"] = "Thanks for your report. Our team will review it shortly.";
            return RedirectToAction(nameof(ViewProfile), new { id });
        }
    }
}