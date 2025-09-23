using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; 
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
        public async Task<IActionResult> UpdateQuantity(int productId, int quantity, string action)
        {
            var user = await _userManager.GetUserAsync(User);
            var item = _context.CartItems.FirstOrDefault(c => c.UserId == user.Id && c.ProductId == productId);
            if (item != null)
            {
                if (action == "increase")
                {
                    item.Quantity++;
                }
                else if (action == "decrease" && item.Quantity > 1)
                {
                    item.Quantity--;
                }
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("MyCart");
        }

        [HttpPost]
        public async Task<IActionResult> RemoveItem(int productId)
        {
            var user = await _userManager.GetUserAsync(User);
            var item = _context.CartItems.FirstOrDefault(c => c.UserId == user.Id && c.ProductId == productId);
            if (item != null)
            {
                _context.CartItems.Remove(item);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("MyCart");
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

            int totalRequested = quantity;
            if (existingItem != null)
            {
                totalRequested += existingItem.Quantity;
            }

            if (totalRequested > product.Quantity)
            {
                TempData["Error"] = $"Cannot add {totalRequested} items. Only {product.Quantity} left in stock.";
                return RedirectToAction("MyCart");
            }

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

        [HttpGet]
        public async Task<IActionResult> Checkout()
        {
            var user = await _userManager.GetUserAsync(User);
            var cartItems = _context.CartItems.Include(c => c.Product).Where(c => c.UserId == user.Id).ToList();
            if (!cartItems.Any())
            {
                TempData["Error"] = "Your cart is empty.";
                return RedirectToAction("MyCart");
            }
            // You can pass address/payment info via ViewModel later
            return View(cartItems);
        }
        [HttpPost]
        public async Task<IActionResult> MpesaPay(string phone, decimal amount)

        {
           
            var user = await _userManager.GetUserAsync(User);
            var cartItems = _context.CartItems.Include(c => c.Product).Where(c => c.UserId == user.Id).ToList();

            if (!cartItems.Any())
            {
                TempData["Error"] = "Your cart is empty.";
                return RedirectToAction("MyCart");
            }

            
            var order = new Order
            {
                UserId = user.Id,
                OrderDate = DateTime.Now,
                TotalAmount = amount,
                PaymentMethod = "Mpesa",
                Status = "Paid"
            };
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Add ordered items to OrderItems table 
            foreach (var item in cartItems)
            {
                // Reduce inventory
                if (item.Product != null)
                {
                    item.Product.Quantity -= item.Quantity;
                }

                // Add to OrderItems
                var orderItem = new OrderItem
                {
                    OrderId = order.Id,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    Price = item.Product?.Price ?? 0
                };
                _context.OrderItems.Add(orderItem);

                // Notify seller
                if (item.Product != null)
                {
                    var sellerId = item.Product.UserId;
                    var notification = new Notification
                    {
                        UserId = sellerId,
                        Title = "New Order Received",
                        Body = $"Order placed for your item '{item.Product.Name}' by {user.UserName} (Quantity: {item.Quantity})",
                        Type = "order",
                        CreatedAt = DateTime.Now
                    };
                    _context.Notifications.Add(notification);
                }
            }

            // Clear cart
            _context.CartItems.RemoveRange(cartItems);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Mpesa payment successful for {phone} (Amount: {amount:C}). Order placed!";
            return RedirectToAction("MyCart");
        }
        
        [HttpGet]
        public async Task<IActionResult> MyOrders()
        {
            var user = await _userManager.GetUserAsync(User);
            var orders = _context.Orders
                .Where(o => o.UserId == user.Id)
                .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                .OrderByDescending(o => o.OrderDate)
                .ToList();
            return View(orders);
        }
    }
}