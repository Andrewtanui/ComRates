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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var product = await _context.Products.FindAsync(productId);
            if (product == null)
            {
                TempData["Error"] = "Product not found.";
                return RedirectToAction("Index", "Products");
            }

            // Check if product already in cart
            var existingItem = await _context.CartItems
                .FirstOrDefaultAsync(c => c.UserId == user.Id && c.ProductId == productId);

            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
            }
            else
            {
                var cartItem = new CartItem
                {
                    UserId = user.Id,
                    ProductId = productId,
                    Quantity = quantity,
                    DateAdded = DateTime.Now
                };
                _context.CartItems.Add(cartItem);
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = $"{product.Name} added to cart!";
            return RedirectToAction("MyCart");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveItem(int productId)
        {
            var user = await _userManager.GetUserAsync(User);
            var cartItem = await _context.CartItems
                .FirstOrDefaultAsync(c => c.UserId == user.Id && c.ProductId == productId);

            if (cartItem != null)
            {
                _context.CartItems.Remove(cartItem);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Item removed from cart.";
            }

            return RedirectToAction("MyCart");
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
        [Authorize(Roles = "Buyer")]
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
        public async Task<IActionResult> Checkout()
        {
            var user = await _userManager.GetUserAsync(User);
            var cartItems = await _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.UserId == user.Id)
                .ToListAsync();

            if (!cartItems.Any())
            {
                TempData["Error"] = "Your cart is empty.";
                return RedirectToAction("MyCart");
            }

            // Get all delivery services
            var deliveryServices = await _context.Users
                .Where(u => u.UserRole == UserRole.DeliveryService)
                .ToListAsync();
            
            ViewBag.DeliveryServices = deliveryServices;

            return View(cartItems);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(
            string paymentMethod, 
            string deliveryAddress, 
            string deliveryTown,
            string deliveryCounty,
            string deliveryServiceId,
            decimal deliveryFee)
        {
            var user = await _userManager.GetUserAsync(User);
            var cartItems = await _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.UserId == user.Id)
                .ToListAsync();

            if (!cartItems.Any())
            {
                TempData["Error"] = "Your cart is empty.";
                return RedirectToAction("MyCart");
            }

            // Calculate total
            var totalAmount = cartItems.Sum(c => c.Product.Price * c.Quantity);

            // Generate tracking number
            var trackingNumber = $"TRK{DateTime.Now:yyyyMMddHHmmss}";

            // Create order
            var order = new Order
            {
                UserId = user.Id,
                OrderDate = DateTime.Now,
                TotalAmount = totalAmount + deliveryFee,
                PaymentMethod = paymentMethod ?? "Cash on Delivery",
                Status = paymentMethod == "Mpesa" ? "Paid" : "Pending",
                DeliveryServiceId = deliveryServiceId,
                DeliveryStatus = "Preparing",
                DeliveryAddress = deliveryAddress,
                DeliveryTown = deliveryTown,
                DeliveryCounty = deliveryCounty,
                DeliveryFee = deliveryFee,
                TrackingNumber = trackingNumber,
                BuyerLatitude = null, // TODO: Get from browser geolocation
                BuyerLongitude = null
            };
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Add order items and update inventory
            foreach (var item in cartItems)
            {
                if (item.Product != null)
                {
                    // Reduce inventory
                    item.Product.Quantity -= item.Quantity;

                    // Add to OrderItems
                    var orderItem = new OrderItem
                    {
                        OrderId = order.Id,
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        Price = item.Product.Price
                    };
                    _context.OrderItems.Add(orderItem);

                    // Notify seller
                    var sellerId = item.Product.UserId;
                    var sellerNotification = new Notification
                    {
                        UserId = sellerId,
                        Title = "New Order Received",
                        Body = $"Order #{order.Id} placed for '{item.Product.Name}' (Qty: {item.Quantity})",
                        Type = "order",
                        CreatedAt = DateTime.Now
                    };
                    _context.Notifications.Add(sellerNotification);
                }
            }

            // Notify delivery service
            if (!string.IsNullOrEmpty(deliveryServiceId))
            {
                var deliveryNotification = new Notification
                {
                    UserId = deliveryServiceId,
                    Title = "New Delivery Assignment",
                    Body = $"New delivery order #{order.Id} to {deliveryTown}, {deliveryCounty}. Tracking: {trackingNumber}",
                    Type = "delivery",
                    CreatedAt = DateTime.Now
                };
                _context.Notifications.Add(deliveryNotification);
            }

            // Clear cart
            _context.CartItems.RemoveRange(cartItems);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Order #{order.Id} placed successfully! Tracking Number: {trackingNumber}";
            return RedirectToAction("MyOrders");
        }

        [HttpGet]
        [Authorize(Roles = "Buyer")]
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