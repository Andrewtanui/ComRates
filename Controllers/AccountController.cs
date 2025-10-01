using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TanuiApp.Models;
using TanuiApp.ViewModels;

namespace UsersApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<Users> signInManager;
        private readonly UserManager<Users> userManager;
        private readonly RoleManager<IdentityRole> roleManager;
        private readonly ILogger<AccountController> logger;

        public AccountController(
            SignInManager<Users> signInManager, 
            UserManager<Users> userManager, 
            RoleManager<IdentityRole> roleManager,
            ILogger<AccountController> logger)
        {
            this.signInManager = signInManager;
            this.userManager = userManager;
            this.roleManager = roleManager;
            this.logger = logger;
        }

        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var result = await signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, false);

                if (result.Succeeded)
                {
                    // Log user role on login for verification
                    var user = await userManager.FindByEmailAsync(model.Email);
                    if (user != null)
                    {
                        var roles = await userManager.GetRolesAsync(user);
                        logger.LogInformation($"User {user.Email} logged in with role: {user.UserRole}, Assigned roles: {string.Join(", ", roles)}");
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
                        users.CompanyName = model.CompanyName;
                        users.LicenseNumber = model.LicenseNumber;
                        users.VehicleInfo = model.VehicleInfo;
                        
                        logger.LogInformation($"Registering delivery service: {model.CompanyName}");
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

                        TempData["SuccessMessage"] = $"Account created successfully! You have been registered as a {roleName}.";
                        return RedirectToAction("Login", "Account");
                    }
                    else
                    {
                        logger.LogError($"User creation failed for {model.Email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                        
                        foreach (var error in result.Errors)
                        {
                            ModelState.AddModelError("", error.Description);
                        }

                        return View(model);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError($"Exception during registration: {ex.Message}");
                    ModelState.AddModelError("", "An error occurred during registration. Please try again.");
                    return View(model);
                }
            }
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
    }
}