using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TanuiApp.Data;
using TanuiApp.Models;
using System.Linq;
using System.Threading.Tasks;

namespace TanuiApp.Controllers
{
    [Authorize]
    public class ProductsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<Users> _userManager;

        public ProductsController(AppDbContext context, UserManager<Users> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [AllowAnonymous]
        public IActionResult Index()
        {
            var products = _context.Products.ToList();
            return View(products);
        }

        [AllowAnonymous]
        public IActionResult Category(string name)
        {
            var products = _context.Products
                .Where(p => p.Category == name)
                .ToList();

            ViewBag.CategoryName = name;
            return View(products);
        }

        // 🔍 SEARCH FEATURE
        [AllowAnonymous]
        public IActionResult Search(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return View("Index", _context.Products.ToList());
            }

            query = query.ToLower();

            var results = _context.Products
                .Where(p =>
                    p.Name.ToLower().Contains(query) ||
                    p.Description.ToLower().Contains(query) ||
                    p.Category.ToLower().Contains(query))
                .ToList();

            ViewBag.SearchQuery = query;

            return View("Index", results);
        }

        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product model, IFormFile ImageFile)
        {
            if (!ModelState.IsValid)
            {
                // ✅ Return errors to the view instead of crashing
                return View(model);
            }

            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    ModelState.AddModelError("", "User not found. Please log in again.");
                    return View(model);
                }

                model.UserId = user.Id;
                model.Rating = 0;
                model.OnSale = false;

                // ✅ Handle Image Upload
                if (ImageFile != null && ImageFile.Length > 0)
                {
                    var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/products");
                    if (!Directory.Exists(uploadDir))
                        Directory.CreateDirectory(uploadDir);

                    var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(ImageFile.FileName);
                    var filePath = Path.Combine(uploadDir, uniqueFileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await ImageFile.CopyToAsync(stream);
                    }

                    model.ImageUrl = "/images/products/" + uniqueFileName;
                }
                else
                {
                    model.ImageUrl = "/images/products/default.png";
                }

                _context.Products.Add(model);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Product created successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                // ✅ Capture the real error instead of silent crash
                ModelState.AddModelError("", $"An error occurred: {ex.Message}");
                return View(model);
            }
        }

        // ---------------- Add to Cart ----------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart(int productId)
        {
            var user = await _userManager.GetUserAsync(User);
            var product = await _context.Products.FindAsync(productId);

            if (product == null) return NotFound();

            if (product.UserId == user.Id)
            {
                TempData["Error"] = "You cannot add your own product to the cart.";
                return RedirectToAction(nameof(Index));
            }

            var existingItem = _context.CartItems
                .FirstOrDefault(c => c.ProductId == productId && c.UserId == user.Id);

            if (existingItem != null)
                existingItem.Quantity++;
            else
                _context.CartItems.Add(new CartItem
                {
                    UserId = user.Id,
                    ProductId = productId,
                    Quantity = 1
                });

            await _context.SaveChangesAsync();
            TempData["Success"] = "Product added to cart!";
            return RedirectToAction(nameof(Index));
        }
    }
}
