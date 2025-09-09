using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TanuiApp.Data;
using TanuiApp.Models;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;

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

        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                model.UserId = user.Id;
                model.Rating = 0;
                model.OnSale = false;

                _context.Products.Add(model);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }
            return View(model);
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
