using Microsoft.AspNetCore.Identity;
using TanuiApp.Models;

namespace TanuiApp.Data
{
    public static class DbSeeder
    {
        public static async Task SeedDefaultAdmin(UserManager<Users> userManager)
        {
        
            var adminExists = userManager.Users.Any(u => u.UserRole == UserRole.SystemAdmin);
            
            if (!adminExists)
            {
                var adminUser = new Users
                {
                    FullName = "System Administrator",
                    Email = "safereturn254@gmail.com",
                    UserName = "safereturn254@gmail.com",
                    EmailConfirmed = true,
                    PhoneNumberConfirmed = true,
                    UserRole = UserRole.SystemAdmin,
                    CreatedAt = DateTime.Now,
                    IsPublicProfile = false,
                    EmailNotifications = true,
                    SmsNotifications = false
                };

                var result = await userManager.CreateAsync(adminUser, "Comrates123");

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "SystemAdmin");
                    Console.WriteLine("✅ Default SystemAdmin created successfully!");
                    Console.WriteLine("   Email: safereturn254@gmail.com");
                    Console.WriteLine("   Password: Comrates123");
                    Console.WriteLine("   ⚠️  Please change the password after first login!");
                }
                else
                {
                    Console.WriteLine("❌ Failed to create SystemAdmin:");
                    foreach (var error in result.Errors)
                    {
                        Console.WriteLine($"   - {error.Description}");
                    }
                }
            }
        }

        // public static async Task SeedTestData(UserManager<Users> userManager)
        // {
            
        //     if (!userManager.Users.Any(u => u.Email == "buyer@test.com"))
        //     {
        //         var buyer = new Users
        //         {
        //             FullName = "Test Buyer",
        //             Email = "buyer@test.com",
        //             UserName = "buyer@test.com",
        //             EmailConfirmed = true,
        //             UserRole = UserRole.Buyer,
        //             CreatedAt = DateTime.Now,
        //             Town = "Nairobi",
        //             County = "Nairobi",
        //             Address = "123 Test Street"
        //         };
        //         await userManager.CreateAsync(buyer, "Buyer@123");
        //         await userManager.AddToRoleAsync(buyer, "Buyer");
        //         Console.WriteLine("✅ Test Buyer created: buyer@test.com / Buyer@123");
        //     }

            
        //     if (!userManager.Users.Any(u => u.Email == "seller@test.com"))
        //     {
        //         var seller = new Users
        //         {
        //             FullName = "Test Seller",
        //             Email = "seller@test.com",
        //             UserName = "seller@test.com",
        //             EmailConfirmed = true,
        //             UserRole = UserRole.Seller,
        //             CreatedAt = DateTime.Now,
        //             Town = "Mombasa",
        //             County = "Mombasa"
        //         };
        //         await userManager.CreateAsync(seller, "Seller@123");
        //         await userManager.AddToRoleAsync(seller, "Seller");
        //         Console.WriteLine("✅ Test Seller created: seller@test.com / Seller@123");
        //     }

        //     // Seed a test delivery service
        //     if (!userManager.Users.Any(u => u.Email == "delivery@test.com"))
        //     {
        //         var delivery = new Users
        //         {
        //             FullName = "Test Delivery Service",
        //             Email = "delivery@test.com",
        //             UserName = "delivery@test.com",
        //             EmailConfirmed = true,
        //             UserRole = UserRole.DeliveryService,
        //             CompanyName = "Swift Deliveries",
        //             LicenseNumber = "DL12345",
        //             VehicleInfo = "Motorcycle - KCA 123A",
        //             IsVerified = true,
        //             CreatedAt = DateTime.Now,
        //             Town = "Nairobi",
        //             County = "Nairobi"
        //         };
        //         await userManager.CreateAsync(delivery, "Delivery@123");
        //         await userManager.AddToRoleAsync(delivery, "DeliveryService");
        //         Console.WriteLine("✅ Test Delivery Service created: delivery@test.com / Delivery@123");
        //     }
        // }
    }
}
