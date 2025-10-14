using Microsoft.AspNetCore.Identity;
using TanuiApp.Models;
using TanuiApp.Services;
using Microsoft.EntityFrameworkCore;

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

        public static async Task SeedChatbotTrainingData(AppDbContext db, IChatbotService chatbot)
        {
            // If we already have a decent number of training rows, skip
            var existingCount = await db.ChatbotTrainingData.CountAsync();
            if (existingCount >= 50) return;

            var samples = new List<ChatbotTrainingData>
            {
                // Greetings
                new() { Text = "hi", Intent = "greeting" },
                new() { Text = "hello", Intent = "greeting" },
                new() { Text = "hey", Intent = "greeting" },

                // Browse & Search
                new() { Text = "show me products", Intent = "browse_products" },
                new() { Text = "browse products", Intent = "browse_products" },
                new() { Text = "search products", Intent = "search_products" },
                new() { Text = "how to search", Intent = "search_products" },
                new() { Text = "categories", Intent = "view_categories" },

                // Cart & Checkout
                new() { Text = "add to cart", Intent = "add_to_cart" },
                new() { Text = "my cart", Intent = "view_cart" },
                new() { Text = "checkout", Intent = "checkout" },

                // Orders
                new() { Text = "my orders", Intent = "my_orders" },
                new() { Text = "track order", Intent = "track_order" },
                new() { Text = "order status", Intent = "order_status" },

                // Shipping
                new() { Text = "shipping", Intent = "shipping" },
                new() { Text = "delivery time", Intent = "delivery_time" },
                new() { Text = "shipping cost", Intent = "shipping_cost" },

                // Returns & Refunds
                new() { Text = "return policy", Intent = "returns" },
                new() { Text = "refund", Intent = "refund" },
                new() { Text = "cancel order", Intent = "cancel_order" },

                // Payments
                new() { Text = "payment methods", Intent = "payment_methods" },
                new() { Text = "pay with mpesa", Intent = "mpesa" },
                new() { Text = "payments", Intent = "payments" },

                // Account & Settings
                new() { Text = "my account", Intent = "my_account" },
                new() { Text = "profile settings", Intent = "profile_settings" },
                new() { Text = "change password", Intent = "change_password" },
                new() { Text = "notifications", Intent = "notifications" },

                // Seller
                new() { Text = "become seller", Intent = "become_seller" },
                new() { Text = "list product", Intent = "list_product" },
                new() { Text = "seller dashboard", Intent = "seller_dashboard" },

                // Support
                new() { Text = "messages", Intent = "messages" },
                new() { Text = "contact support", Intent = "customer_support" },
                new() { Text = "help", Intent = "help" },

                // Reviews & Pricing
                new() { Text = "write review", Intent = "leave_review" },
                new() { Text = "product reviews", Intent = "view_reviews" },
                new() { Text = "pricing", Intent = "pricing" },
                new() { Text = "deals", Intent = "deals" },

                // Technical & General
                new() { Text = "can't login", Intent = "login_issue" },
                new() { Text = "payment failed", Intent = "payment_failed" },
                new() { Text = "page error", Intent = "page_error" },
                new() { Text = "how it works", Intent = "how_it_works" },
                new() { Text = "about comrates", Intent = "about" },
                new() { Text = "is it safe", Intent = "safety" },
            };

            // Normalize to lowercase to match model pipeline expectations
            foreach (var s in samples)
            {
                s.Text = s.Text.ToLower();
                s.Intent = s.Intent.ToLower();
                s.IsActive = true;
                s.CreatedAt = DateTime.UtcNow;
            }

            // Only insert ones that don't already exist
            var existing = await db.ChatbotTrainingData
                .Select(t => new { t.Text, t.Intent })
                .ToListAsync();

            var toInsert = samples
                .Where(s => !existing.Any(e => e.Text == s.Text && e.Intent == s.Intent))
                .ToList();

            if (toInsert.Count > 0)
            {
                db.ChatbotTrainingData.AddRange(toInsert);
                await db.SaveChangesAsync();

                // Retrain model after seeding
                await chatbot.RetrainModelAsync();
                Console.WriteLine($"✅ Seeded {toInsert.Count} chatbot training samples and retrained the model.");
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
