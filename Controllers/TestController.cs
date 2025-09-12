using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TanuiApp.Data;
using TanuiApp.Models;

namespace TanuiApp.Controllers
{
    [Authorize]
    public class TestController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<Users> _userManager;
        private readonly ILogger<TestController> _logger;

        public TestController(AppDbContext context, UserManager<Users> userManager, ILogger<TestController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        // Test 1: Basic page load
        public IActionResult Index()
        {
            _logger.LogInformation("Test controller index accessed");
            return View();
        }

        // Test 2: Database connection
        public async Task<IActionResult> TestDb()
        {
            try
            {
                _logger.LogInformation("Testing database connection...");

                var canConnect = await _context.Database.CanConnectAsync();
                _logger.LogInformation("Database connection result: {CanConnect}", canConnect);

                var productCount = await _context.Products.CountAsync();
                _logger.LogInformation("Product count: {ProductCount}", productCount);

                ViewBag.Message = $"Database OK. Products: {productCount}";
                return View("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database test failed");
                ViewBag.Message = $"Database Error: {ex.Message}";
                return View("Index");
            }
        }

        // Test 3: User authentication
        public async Task<IActionResult> TestAuth()
        {
            try
            {
                _logger.LogInformation("Testing user authentication...");

                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    ViewBag.Message = "User not found - authentication issue";
                }
                else
                {
                    ViewBag.Message = $"User OK: {user.UserName} (ID: {user.Id})";
                }

                return View("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Auth test failed");
                ViewBag.Message = $"Auth Error: {ex.Message}";
                return View("Index");
            }
        }

        // Test 4: Simple product creation (no file upload)
        public IActionResult CreateSimple()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSimple(string name, decimal price, string category)
        {
            try
            {
                _logger.LogInformation("Starting simple product creation test");
                _logger.LogInformation("Name: {Name}, Price: {Price}, Category: {Category}", name, price, category);

                if (string.IsNullOrWhiteSpace(name))
                {
                    ViewBag.Message = "Name is required";
                    return View();
                }

                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    ViewBag.Message = "User not found";
                    return View();
                }

                _logger.LogInformation("User found: {UserId}", user.Id);

                var product = new Product
                {
                    Name = name,
                    Price = price,
                    Category = category ?? "Test",
                    Description = "Test product",
                    ImageUrl = "/images/products/default.png",
                    UserId = user.Id,
                    Rating = 0,
                    OnSale = false,
                    CreatedAt = DateTime.UtcNow
                };

                _logger.LogInformation("Adding product to context...");
                _context.Products.Add(product);

                _logger.LogInformation("Saving changes to database...");
                await _context.SaveChangesAsync();

                _logger.LogInformation("Product created successfully with ID: {ProductId}", product.Id);
                ViewBag.Message = $"Success! Product created with ID: {product.Id}";
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Simple product creation failed");
                ViewBag.Message = $"Error: {ex.Message}";
                if (ex.InnerException != null)
                {
                    ViewBag.Message += $" Inner: {ex.InnerException.Message}";
                }
                return View("Index");
            }
        }

        // Test 5: File system test
        public async Task<IActionResult> TestFileSystemAsync()
        {
            try
            {
                _logger.LogInformation("Testing file system access...");

                var wwwrootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                var imagesPath = Path.Combine(wwwrootPath, "images");
                var productsPath = Path.Combine(imagesPath, "products");

                _logger.LogInformation("WWWRoot path: {Path}", wwwrootPath);
                _logger.LogInformation("Images path: {Path}", imagesPath);
                _logger.LogInformation("Products path: {Path}", productsPath);

                // Check if directories exist
                var wwwExists = Directory.Exists(wwwrootPath);
                var imagesExists = Directory.Exists(imagesPath);
                var productsExists = Directory.Exists(productsPath);

                _logger.LogInformation("WWWRoot exists: {Exists}", wwwExists);
                _logger.LogInformation("Images exists: {Exists}", imagesExists);
                _logger.LogInformation("Products exists: {Exists}", productsExists);

                // Try to create directories
                if (!Directory.Exists(productsPath))
                {
                    Directory.CreateDirectory(productsPath);
                    _logger.LogInformation("Created products directory");
                }

                // Test write permission
                var testFile = Path.Combine(productsPath, "test.txt");
                await System.IO.File.WriteAllTextAsync(testFile, "Test file content");
                _logger.LogInformation("Test file written successfully");

                // Clean up
                if (System.IO.File.Exists(testFile))
                {
                    System.IO.File.Delete(testFile);
                    _logger.LogInformation("Test file deleted");
                }

                ViewBag.Message = "File system test passed!";
                return View("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "File system test failed");
                ViewBag.Message = $"File System Error: {ex.Message}";
                return View("Index");
            }
        }
    }
}