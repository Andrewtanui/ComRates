using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TanuiApp.Data;
using TanuiApp.Models;
using TanuiApp.ViewModels;
using TanuiApp.Services;

namespace UsersApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<Users> signInManager;
        private readonly UserManager<Users> userManager;
        private readonly RoleManager<IdentityRole> roleManager;
        private readonly ILogger<AccountController> logger;
        private readonly AppDbContext context;
        private readonly IEmailSender emailSender;
        private readonly IOtpService otpService;

        public AccountController(
            SignInManager<Users> signInManager, 
            UserManager<Users> userManager, 
            RoleManager<IdentityRole> roleManager,
            ILogger<AccountController> logger,
            AppDbContext context,
            IEmailSender emailSender,
            IOtpService otpService)
        {
            this.signInManager = signInManager;
            this.userManager = userManager;
            this.roleManager = roleManager;
            this.logger = logger;
            this.context = context;
            this.emailSender = emailSender;
            this.otpService = otpService;
        }

        public IActionResult Login(bool banned = false, bool suspended = false, string? reason = null)
        {
            if (banned)
            {
                ModelState.AddModelError("", $"Your account has been permanently banned. Reason: {reason ?? "Violation of terms"}. This account cannot be used again.");
            }
            else if (suspended)
            {
                ModelState.AddModelError("", $"Your account has been suspended. Reason: {reason ?? "Under review"}. Please contact support.");
            }
            
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                // First check if user exists and credentials are valid
                var user = await userManager.FindByEmailAsync(model.Email);
                if (user != null)
                {
                    // Check if user is banned
                    if (user.IsBanned)
                    {
                        ModelState.AddModelError("", $"Your account has been permanently banned. Reason: {user.BanReason ?? "Violation of terms"}. This email cannot be used again.");
                        return View(model);
                    }

                    // Check if user is suspended
                    if (user.IsSuspended)
                    {
                        ModelState.AddModelError("", $"Your account has been suspended. Reason: {user.SuspensionReason ?? "Under review"}. Please contact support.");
                        return View(model);
                    }

                    // Check if email is verified
                    if (!user.EmailVerified)
                    {
                        // Generate new OTP and send it
                        var otp = otpService.GenerateOTP();
                        user.EmailVerificationOTP = otp;
                        user.OTPExpiryTime = DateTime.Now.AddMinutes(10);
                        await userManager.UpdateAsync(user);

                        // Send OTP email
                        var emailBody = $@"
                            <h2>Email Verification Required</h2>
                            <p>Hello {user.FullName},</p>
                            <p>Your account is not verified. Please use the following OTP to verify your email:</p>
                            <h1 style='color: #4CAF50; font-size: 32px; letter-spacing: 5px;'>{otp}</h1>
                            <p>This OTP will expire in 10 minutes.</p>
                            <p>If you did not request this, please ignore this email.</p>
                        ";
                        
                        try
                        {
                            await emailSender.SendEmailAsync(user.Email, "Verify Your Email - ComRates", emailBody);
                            TempData["Email"] = user.Email;
                            TempData["InfoMessage"] = "Your account is not verified. An OTP has been sent to your email.";
                            return RedirectToAction("VerifyOtp");
                        }
                        catch (Exception ex)
                        {
                            logger.LogError($"Failed to send OTP email: {ex.Message}");
                            ModelState.AddModelError("", "Failed to send verification email. Please try again later.");
                            return View(model);
                        }
                    }
                }

                var result = await signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, false);

                if (result.Succeeded)
                {
                    // Log user role on login for verification
                    if (user != null)
                    {
                        var roles = await userManager.GetRolesAsync(user);
                        logger.LogInformation($"User {user.Email} logged in with role: {user.UserRole}, Assigned roles: {string.Join(", ", roles)}");
                        
                        // Redirect users based on their role
                        if (user.UserRole == UserRole.SystemAdmin)
                        {
                            return RedirectToAction("Index", "AdminDashboard");
                        }
                        else if (user.UserRole == UserRole.Seller)
                        {
                            return RedirectToAction("Index", "SellerDashboard");
                        }
                        else if (user.UserRole == UserRole.DeliveryService)
                        {
                            return RedirectToAction("Index", "DeliveryDashboard");
                        }
                    }
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    ModelState.AddModelError("", "Email or password is incorrect.");
                    return View(model);
                }
            }
            return View(model);
        }

        public IActionResult Register()
        {
            // Load active delivery companies for the dropdown
            ViewBag.DeliveryCompanies = context.DeliveryCompanies
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToList();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Check if user already exists
                    var existingUser = await userManager.FindByEmailAsync(model.Email);
                    if (existingUser != null)
                    {
                        // Check if the existing account is banned
                        if (existingUser.IsBanned)
                        {
                            ModelState.AddModelError("Email", "This email has been permanently banned and cannot be used to create an account.");
                            return View(model);
                        }
                        
                        ModelState.AddModelError("Email", "An account with this email already exists.");
                        return View(model);
                    }

                    Users users = new Users
                    {
                        FullName = model.Name,
                        Email = model.Email,
                        UserName = model.Email,
                        PhoneNumber = model.PhoneNumber,
                        DateOfBirth = model.DateOfBirth,
                        Address = model.Address,
                        Estate = model.Estate,
                        Town = model.Town,
                        County = model.County,
                        PostalCode = model.PostalCode,
                        EmailNotifications = model.EmailNotifications,
                        SmsNotifications = model.SmsNotifications,
                        IsPublicProfile = model.IsPublicProfile,
                        Bio = model.Bio,
                        ProfilePictureUrl = model.ProfilePictureUrl,
                        UserRole = model.UserRole,
                        CreatedAt = DateTime.Now
                    };

                    // Handle delivery service specific fields
                    if (model.UserRole == UserRole.DeliveryService)
                    {
                        // CompanyName and LicenseNumber are admin-managed; do not bind from registration form
                        users.VehicleInfo = model.VehicleInfo;
                        users.DeliveryCompanyId = model.DeliveryCompanyId;
                        if (model.DeliveryCompanyId.HasValue)
                        {
                            var company = await context.DeliveryCompanies.FirstOrDefaultAsync(c => c.Id == model.DeliveryCompanyId.Value);
                            if (company != null)
                            {
                                // If company name not provided, default to selected delivery company name
                                if (string.IsNullOrWhiteSpace(users.CompanyName))
                                {
                                    users.CompanyName = company.Name;
                                }
                            }
                        }
                        
                        logger.LogInformation($"Registering delivery service. DeliveryCompanyId: {model.DeliveryCompanyId}, VehicleInfo: {model.VehicleInfo}");
                    }

                    // Create user
                    var result = await userManager.CreateAsync(users, model.Password);

                    if (result.Succeeded)
                    {
                        logger.LogInformation($"User created successfully: {users.Email} with UserRole: {users.UserRole}");

                        // Ensure role exists and assign it
                        string roleName = model.UserRole.ToString();
                        
                        var roleExists = await roleManager.RoleExistsAsync(roleName);
                        if (!roleExists)
                        {
                            var roleResult = await roleManager.CreateAsync(new IdentityRole(roleName));
                            if (roleResult.Succeeded)
                            {
                                logger.LogInformation($"Role created: {roleName}");
                            }
                            else
                            {
                                logger.LogError($"Failed to create role {roleName}: {string.Join(", ", roleResult.Errors.Select(e => e.Description))}");
                            }
                        }
                        else
                        {
                            logger.LogInformation($"Role already exists: {roleName}");
                        }

                        // Assign role to user
                        var addToRoleResult = await userManager.AddToRoleAsync(users, roleName);
                        if (addToRoleResult.Succeeded)
                        {
                            logger.LogInformation($"User {users.Email} assigned to role: {roleName}");
                            
                            // Verify role assignment
                            var userRoles = await userManager.GetRolesAsync(users);
                            logger.LogInformation($"User {users.Email} roles after assignment: {string.Join(", ", userRoles)}");
                        }
                        else
                        {
                            logger.LogError($"Failed to assign role {roleName} to user {users.Email}: {string.Join(", ", addToRoleResult.Errors.Select(e => e.Description))}");
                        }

                        // Generate OTP and send verification email
                        var otp = otpService.GenerateOTP();
                        users.EmailVerificationOTP = otp;
                        users.OTPExpiryTime = DateTime.Now.AddMinutes(10);
                        users.EmailVerified = false;
                        await userManager.UpdateAsync(users);

                        var emailBody = $@"
                            <h2>Welcome to ComRates!</h2>
                            <p>Hello {users.FullName},</p>
                            <p>Thank you for registering. Please verify your email using the OTP below:</p>
                            <h1 style='color: #4CAF50; font-size: 32px; letter-spacing: 5px;'>{otp}</h1>
                            <p>This OTP will expire in 10 minutes.</p>
                            <p>If you did not create this account, please ignore this email.</p>
                        ";

                        try
                        {
                            await emailSender.SendEmailAsync(users.Email, "Verify Your Email - ComRates", emailBody);
                            TempData["Email"] = users.Email;
                            TempData["SuccessMessage"] = $"Account created successfully! An OTP has been sent to {users.Email}. Please verify your email.";
                            return RedirectToAction("VerifyOtp", "Account");
                        }
                        catch (Exception ex)
                        {
                            logger.LogError($"Failed to send verification email: {ex.Message}");
                            TempData["WarningMessage"] = "Account created but failed to send verification email. Please contact support.";
                            return RedirectToAction("Login", "Account");
                        }
                    }
                    else
                    {
                        logger.LogError($"User creation failed for {model.Email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                        
                        foreach (var error in result.Errors)
                        {
                            ModelState.AddModelError("", error.Description);
                        }

                        // Repopulate companies when returning with errors
                        ViewBag.DeliveryCompanies = context.DeliveryCompanies
                            .Where(c => c.IsActive)
                            .OrderBy(c => c.Name)
                            .ToList();
                        return View(model);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError($"Exception during registration: {ex.Message}");
                    ModelState.AddModelError("", "An error occurred during registration. Please try again.");
                    // Repopulate companies on exception
                    ViewBag.DeliveryCompanies = context.DeliveryCompanies
                        .Where(c => c.IsActive)
                        .OrderBy(c => c.Name)
                        .ToList();
                    return View(model);
                }
            }
            // Repopulate companies when model state is invalid
            ViewBag.DeliveryCompanies = context.DeliveryCompanies
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToList();
            return View(model);
        }

        // Test endpoint to verify roles (Remove in production)
        public async Task<IActionResult> TestRoles()
        {
            var testData = new System.Text.StringBuilder();
            testData.AppendLine("=== ROLE VERIFICATION TEST ===\n");

            // Check all expected roles
            var expectedRoles = new[] { "Buyer", "Seller", "DeliveryService", "SystemAdmin" };
            
            testData.AppendLine("Checking if roles exist:");
            foreach (var roleName in expectedRoles)
            {
                var exists = await roleManager.RoleExistsAsync(roleName);
                testData.AppendLine($"- {roleName}: {(exists ? "EXISTS" : "MISSING")}");
            }

            testData.AppendLine("\nAll roles in system:");
            var allRoles = roleManager.Roles.ToList();
            foreach (var role in allRoles)
            {
                var usersInRole = await userManager.GetUsersInRoleAsync(role.Name);
                testData.AppendLine($"- {role.Name} (ID: {role.Id}) - {usersInRole.Count} users");
            }

            testData.AppendLine("\nAll users and their roles:");
            var allUsers = userManager.Users.ToList();
            foreach (var user in allUsers)
            {
                var roles = await userManager.GetRolesAsync(user);
                testData.AppendLine($"- {user.Email} (UserRole enum: {user.UserRole}) - Assigned roles: {string.Join(", ", roles)}");
            }

            return Content(testData.ToString(), "text/plain");
        }

        // Initialize all roles (call this once on app startup)
        public async Task<IActionResult> InitializeRoles()
        {
            var roles = new[] { "Buyer", "Seller", "DeliveryService", "SystemAdmin" };
            var result = new System.Text.StringBuilder();
            
            result.AppendLine("Initializing roles...\n");

            foreach (var roleName in roles)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    var roleResult = await roleManager.CreateAsync(new IdentityRole(roleName));
                    result.AppendLine($"Created role: {roleName} - {(roleResult.Succeeded ? "SUCCESS" : "FAILED")}");
                    
                    if (!roleResult.Succeeded)
                    {
                        foreach (var error in roleResult.Errors)
                        {
                            result.AppendLine($"  Error: {error.Description}");
                        }
                    }
                }
                else
                {
                    result.AppendLine($"Role already exists: {roleName}");
                }
            }

            return Content(result.ToString(), "text/plain");
        }

        public IActionResult VerifyEmail()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> VerifyEmail(VerifyEmailViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await userManager.FindByNameAsync(model.Email);

                if (user == null)
                {
                    ModelState.AddModelError("", "Something is wrong!");
                    return View(model);
                }
                else
                {
                    return RedirectToAction("ChangePassword", "Account", new { username = user.UserName });
                }
            }
            return View(model);
        }

        public IActionResult ChangePassword(string username)
        {
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("VerifyEmail", "Account");
            }
            return View(new ChangePasswordViewModel { Email = username });
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await userManager.FindByNameAsync(model.Email);
                if (user != null)
                {
                    var result = await userManager.RemovePasswordAsync(user);
                    if (result.Succeeded)
                    {
                        result = await userManager.AddPasswordAsync(user, model.NewPassword);
                        return RedirectToAction("Login", "Account");
                    }
                    else
                    {
                        foreach (var error in result.Errors)
                        {
                            ModelState.AddModelError("", error.Description);
                        }

                        return View(model);
                    }
                }
                else
                {
                    ModelState.AddModelError("", "Email not found!");
                    return View(model);
                }
            }
            else
            {
                ModelState.AddModelError("", "Something went wrong. try again.");
                return View(model);
            }
        }

        public async Task<IActionResult> Logout()
        {
            await signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        // OTP Verification Actions
        public IActionResult VerifyOtp()
        {
            var email = TempData["Email"]?.ToString();
            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction("Login");
            }
            
            // Keep email in TempData for the POST action
            TempData.Keep("Email");
            
            return View(new VerifyOtpViewModel { Email = email });
        }

        [HttpPost]
        public async Task<IActionResult> VerifyOtp(VerifyOtpViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await userManager.FindByEmailAsync(model.Email);
                if (user == null)
                {
                    ModelState.AddModelError("", "User not found.");
                    return View(model);
                }

                // Validate OTP
                if (otpService.ValidateOTP(user.EmailVerificationOTP, user.OTPExpiryTime, model.OTP))
                {
                    // Mark email as verified
                    user.EmailVerified = true;
                    user.EmailVerificationOTP = null;
                    user.OTPExpiryTime = null;
                    await userManager.UpdateAsync(user);

                    logger.LogInformation($"Email verified successfully for user: {user.Email}");
                    TempData["SuccessMessage"] = "Email verified successfully! You can now log in.";
                    return RedirectToAction("Login");
                }
                else
                {
                    if (user.OTPExpiryTime.HasValue && DateTime.Now > user.OTPExpiryTime.Value)
                    {
                        ModelState.AddModelError("", "OTP has expired. Please request a new one.");
                    }
                    else
                    {
                        ModelState.AddModelError("", "Invalid OTP. Please try again.");
                    }
                    return View(model);
                }
            }
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ResendOtp(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                TempData["ErrorMessage"] = "Email is required.";
                return RedirectToAction("Login");
            }

            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction("Login");
            }

            // Generate new OTP
            var otp = otpService.GenerateOTP();
            user.EmailVerificationOTP = otp;
            user.OTPExpiryTime = DateTime.Now.AddMinutes(10);
            await userManager.UpdateAsync(user);

            var emailBody = $@"
                <h2>Email Verification OTP</h2>
                <p>Hello {user.FullName},</p>
                <p>You requested a new OTP. Please use the following code to verify your email:</p>
                <h1 style='color: #4CAF50; font-size: 32px; letter-spacing: 5px;'>{otp}</h1>
                <p>This OTP will expire in 10 minutes.</p>
                <p>If you did not request this, please ignore this email.</p>
            ";

            try
            {
                await emailSender.SendEmailAsync(user.Email, "New Verification OTP - ComRates", emailBody);
                TempData["Email"] = user.Email;
                TempData["SuccessMessage"] = "A new OTP has been sent to your email.";
                logger.LogInformation($"New OTP sent to user: {user.Email}");
            }
            catch (Exception ex)
            {
                logger.LogError($"Failed to resend OTP email: {ex.Message}");
                TempData["ErrorMessage"] = "Failed to send OTP. Please try again later.";
            }

            return RedirectToAction("VerifyOtp");
        }
    }
}