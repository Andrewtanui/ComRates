using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // ✅ Add this using statement
using TanuiApp.Data;
using TanuiApp.Models;

namespace TanuiApp.Controllers
{
    [Authorize]
    public class CartController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<Users> _userManager;

        public CartController(AppDbContext context, UserManager<Users> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> MyCart()
        {
            var user = await _userManager.GetUserAsync(User);

            // ✅ Fixed: Include the Product navigation property
            var cartItems = _context.CartItems
                .Include(c => c.Product) // This loads the related Product data
                .Where(c => c.UserId == user.Id)
                .ToList(); // Remove the unnecessary Select projection

            return View(cartItems);
        }

        [HttpPost]
        public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
        {
            var user = await _userManager.GetUserAsync(User);

            var product = _context.Products.Find(productId);

            if (product == null || product.UserId == user.Id)
            {
                return BadRequest("Cannot add this product.");
            }

            var existingItem = _context.CartItems
                .FirstOrDefault(c => c.UserId == user.Id && c.ProductId == productId);

            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
            }
            else
            {
                _context.CartItems.Add(new CartItem
                {
                    UserId = user.Id,
                    ProductId = productId,
                    Quantity = quantity
                });
            }

            await _context.SaveChangesAsync();

            return RedirectToAction("MyCart");
        }
    }
}