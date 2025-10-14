using Microsoft.AspNetCore.Identity;
using TanuiApp.Models;

namespace TanuiApp.Middleware
{
    /// <summary>
    /// Middleware that checks if the authenticated user is banned and logs them out immediately
    /// </summary>
    public class BanCheckMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<BanCheckMiddleware> _logger;

        public BanCheckMiddleware(RequestDelegate next, ILogger<BanCheckMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, UserManager<Users> userManager, SignInManager<Users> signInManager)
        {
            // Only check authenticated users
            if (context.User?.Identity?.IsAuthenticated == true)
            {
                var userId = userManager.GetUserId(context.User);
                
                if (!string.IsNullOrEmpty(userId))
                {
                    var user = await userManager.FindByIdAsync(userId);
                    
                    if (user != null && user.IsBanned)
                    {
                        _logger.LogWarning("Banned user {UserId} ({Email}) attempted to access {Path}. Logging out immediately.", 
                            userId, user.Email, context.Request.Path);
                        
                        // Sign out the user immediately
                        await signInManager.SignOutAsync();
                        
                        // Redirect to login page with ban message
                        context.Response.Redirect($"/Account/Login?banned=true&reason={Uri.EscapeDataString(user.BanReason ?? "Account violation")}");
                        return;
                    }
                    
                    // Also check for suspended users
                    if (user != null && user.IsSuspended)
                    {
                        _logger.LogWarning("Suspended user {UserId} ({Email}) attempted to access {Path}. Logging out immediately.", 
                            userId, user.Email, context.Request.Path);
                        
                        // Sign out the user immediately
                        await signInManager.SignOutAsync();
                        
                        // Redirect to login page with suspension message
                        context.Response.Redirect($"/Account/Login?suspended=true&reason={Uri.EscapeDataString(user.SuspensionReason ?? "Account under review")}");
                        return;
                    }
                }
            }
            
            // Continue to next middleware
            await _next(context);
        }
    }
    
    /// <summary>
    /// Extension method to register the BanCheckMiddleware
    /// </summary>
    public static class BanCheckMiddlewareExtensions
    {
        public static IApplicationBuilder UseBanCheck(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<BanCheckMiddleware>();
        }
    }
}
