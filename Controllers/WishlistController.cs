using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TanuiApp.Data;
using TanuiApp.Models;

namespace TanuiApp.Controllers
{
    [Authorize]
    public class WishlistController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<Users> _userManager;

        public WishlistController(AppDbContext context, UserManager<Users> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Display wishlist page
        public async Task<IActionResult> MyWishlist()
        {
            var user = await _userManager.GetUserAsync(User);

            var wishlistItems = await _context.WishlistItems
                .Include(w => w.Product)
                .Where(w => w.UserId == user.Id)
                .OrderByDescending(w => w.DateAdded)
                .ToListAsync();

            return View(wishlistItems);
        }

        // Add item to wishlist
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToWishlist(int productId)
        {
            var user = await _userManager.GetUserAsync(User);
            var product = await _context.Products.FindAsync(productId);

            if (product == null)
            {
                TempData["Error"] = "Product not found.";
                return RedirectToAction("Index", "Products");
            }

            // Check if user is trying to add their own product
            if (product.UserId == user.Id)
            {
                TempData["Error"] = "You cannot add your own product to wishlist.";
                return RedirectToAction("Index", "Products");
            }

            // Check if item already exists in wishlist
            var existingItem = await _context.WishlistItems
                .FirstOrDefaultAsync(w => w.ProductId == productId && w.UserId == user.Id);

            if (existingItem != null)
            {
                TempData["Info"] = "Product is already in your wishlist.";
                return RedirectToAction("Index", "Products");
            }

            // Add to wishlist
            var wishlistItem = new WishlistItem
            {
                UserId = user.Id,
                ProductId = productId,
                DateAdded = DateTime.Now
            };

            _context.WishlistItems.Add(wishlistItem);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Product added to wishlist!";
            return RedirectToAction("Index", "Products");
        }

        // Remove item from wishlist
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveFromWishlist(int wishlistItemId)
        {
            var user = await _userManager.GetUserAsync(User);

            var wishlistItem = await _context.WishlistItems
                .FirstOrDefaultAsync(w => w.Id == wishlistItemId && w.UserId == user.Id);

            if (wishlistItem == null)
            {
                TempData["Error"] = "Wishlist item not found.";
                return RedirectToAction("MyWishlist");
            }

            _context.WishlistItems.Remove(wishlistItem);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Product removed from wishlist!";
            return RedirectToAction("MyWishlist");
        }

        // Move item from wishlist to cart
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MoveToCart(int wishlistItemId)
        {
            var user = await _userManager.GetUserAsync(User);

            var wishlistItem = await _context.WishlistItems
                .Include(w => w.Product)
                .FirstOrDefaultAsync(w => w.Id == wishlistItemId && w.UserId == user.Id);

            if (wishlistItem == null)
            {
                TempData["Error"] = "Wishlist item not found.";
                return RedirectToAction("MyWishlist");
            }

            // Check if item already exists in cart
            var existingCartItem = await _context.CartItems
                .FirstOrDefaultAsync(c => c.ProductId == wishlistItem.ProductId && c.UserId == user.Id);

            if (existingCartItem != null)
            {
                existingCartItem.Quantity += 1;
            }
            else
            {
                var cartItem = new CartItem
                {
                    UserId = user.Id,
                    ProductId = wishlistItem.ProductId,
                    Quantity = 1,
                    DateAdded = DateTime.Now
                };
                _context.CartItems.Add(cartItem);
            }

            // Remove from wishlist
            _context.WishlistItems.Remove(wishlistItem);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Product moved to cart!";
            return RedirectToAction("MyWishlist");
        }
    }
}