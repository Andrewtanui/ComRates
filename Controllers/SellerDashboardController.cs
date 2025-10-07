using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TanuiApp.Data;
using TanuiApp.Models;
using TanuiApp.ViewModels;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace TanuiApp.Controllers
{
    [Authorize(Roles = "Seller,SystemAdmin")]
    public class SellerDashboardController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<Users> _userManager;
        private readonly ILogger<SellerDashboardController> _logger;

        public SellerDashboardController(
            AppDbContext context, 
            UserManager<Users> userManager,
            ILogger<SellerDashboardController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var viewModel = new SellerDashboardViewModel();

            // Get seller's products
            var sellerProducts = await _context.Products
                .Where(p => p.UserId == userId)
                .ToListAsync();

            viewModel.TotalProducts = sellerProducts.Count;
            viewModel.LowStockProducts = sellerProducts.Count(p => p.Quantity > 0 && p.Quantity <= 10);
            viewModel.OutOfStockProducts = sellerProducts.Count(p => p.Quantity == 0);

            // Get all orders containing seller's products
            var sellerProductIds = sellerProducts.Select(p => p.Id).ToList();
            
            var ordersWithSellerProducts = await _context.Orders
                .Include(o => o.Items)
                    .ThenInclude(oi => oi.Product)
                .Include(o => o.Buyer)
                .Where(o => o.Items.Any(oi => sellerProductIds.Contains(oi.ProductId)))
                .ToListAsync();

            // Calculate revenue and orders
            var sellerOrderItems = ordersWithSellerProducts
                .SelectMany(o => o.Items)
                .Where(oi => sellerProductIds.Contains(oi.ProductId))
                .ToList();

            viewModel.TotalRevenue = sellerOrderItems.Sum(oi => oi.Price * oi.Quantity);
            viewModel.TotalOrders = ordersWithSellerProducts.Count;
            viewModel.AverageOrderValue = viewModel.TotalOrders > 0 
                ? viewModel.TotalRevenue / viewModel.TotalOrders 
                : 0;

            // Top selling products
            var productSales = sellerOrderItems
                .GroupBy(oi => oi.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    UnitsSold = g.Sum(oi => oi.Quantity),
                    Revenue = g.Sum(oi => oi.Price * oi.Quantity)
                })
                .OrderByDescending(x => x.UnitsSold)
                .Take(5)
                .ToList();

            foreach (var sale in productSales)
            {
                var product = sellerProducts.FirstOrDefault(p => p.Id == sale.ProductId);
                if (product != null)
                {
                    viewModel.TopSellingProducts.Add(new ProductPerformance
                    {
                        ProductId = product.Id,
                        ProductName = product.Name,
                        UnitsSold = sale.UnitsSold,
                        Revenue = sale.Revenue,
                        CurrentStock = product.Quantity,
                        ImageUrl = product.ImageUrls.FirstOrDefault() ?? product.ImageUrl
                    });
                }
            }

            // Low performing products (products with sales but low volume)
            var lowPerformers = productSales
                .OrderBy(x => x.UnitsSold)
                .Take(5)
                .ToList();

            foreach (var sale in lowPerformers)
            {
                var product = sellerProducts.FirstOrDefault(p => p.Id == sale.ProductId);
                if (product != null)
                {
                    viewModel.LowPerformingProducts.Add(new ProductPerformance
                    {
                        ProductId = product.Id,
                        ProductName = product.Name,
                        UnitsSold = sale.UnitsSold,
                        Revenue = sale.Revenue,
                        CurrentStock = product.Quantity,
                        ImageUrl = product.ImageUrls.FirstOrDefault() ?? product.ImageUrl
                    });
                }
            }

            // Sales by day of week
            var salesByDay = ordersWithSellerProducts
                .GroupBy(o => o.OrderDate.DayOfWeek)
                .Select(g => new DailySalesData
                {
                    DayName = g.Key.ToString(),
                    OrderCount = g.Count(),
                    Revenue = g.SelectMany(o => o.Items)
                        .Where(oi => sellerProductIds.Contains(oi.ProductId))
                        .Sum(oi => oi.Price * oi.Quantity)
                })
                .OrderBy(d => (int)Enum.Parse<DayOfWeek>(d.DayName))
                .ToList();

            viewModel.SalesByDay = salesByDay;

            // Sales by hour
            var salesByHour = ordersWithSellerProducts
                .GroupBy(o => o.OrderDate.Hour)
                .Select(g => new HourlySalesData
                {
                    Hour = g.Key,
                    TimeRange = $"{g.Key:D2}:00 - {g.Key:D2}:59",
                    OrderCount = g.Count(),
                    Revenue = g.SelectMany(o => o.Items)
                        .Where(oi => sellerProductIds.Contains(oi.ProductId))
                        .Sum(oi => oi.Price * oi.Quantity)
                })
                .OrderBy(h => h.Hour)
                .ToList();

            viewModel.SalesByHour = salesByHour;

            // Recent orders
            var recentOrders = ordersWithSellerProducts
                .OrderByDescending(o => o.OrderDate)
                .Take(10)
                .Select(o => new RecentOrder
                {
                    OrderId = o.Id,
                    OrderDate = o.OrderDate,
                    CustomerName = o.Buyer != null && !string.IsNullOrWhiteSpace(o.Buyer.FullName)
                        ? o.Buyer.FullName
                        : (o.Buyer != null && !string.IsNullOrWhiteSpace(o.Buyer.Email) ? o.Buyer.Email : o.UserId),
                    TotalAmount = o.Items
                        .Where(oi => sellerProductIds.Contains(oi.ProductId))
                        .Sum(oi => oi.Price * oi.Quantity),
                    Status = o.Status,
                    ItemCount = o.Items.Count(oi => sellerProductIds.Contains(oi.ProductId))
                })
                .ToList();

            viewModel.RecentOrders = recentOrders;

            // Stock alerts
            var stockAlerts = sellerProducts
                .Where(p => p.Quantity <= 10)
                .OrderBy(p => p.Quantity)
                .Select(p => new ProductStockAlert
                {
                    ProductId = p.Id,
                    ProductName = p.Name,
                    CurrentStock = p.Quantity,
                    AlertLevel = p.Quantity == 0 ? "Out" : "Low",
                    ImageUrl = p.ImageUrls.FirstOrDefault() ?? p.ImageUrl
                })
                .ToList();

            viewModel.StockAlerts = stockAlerts;

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> GetChartData(string type)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            var sellerProductIds = await _context.Products
                .Where(p => p.UserId == userId)
                .Select(p => p.Id)
                .ToListAsync();

            var ordersWithSellerProducts = await _context.Orders
                .Include(o => o.Items)
                .Where(o => o.Items.Any(oi => sellerProductIds.Contains(oi.ProductId)))
                .ToListAsync();

            if (type == "daily")
            {
                var data = ordersWithSellerProducts
                    .GroupBy(o => o.OrderDate.DayOfWeek)
                    .Select(g => new
                    {
                        day = g.Key.ToString(),
                        orders = g.Count(),
                        revenue = g.SelectMany(o => o.Items)
                            .Where(oi => sellerProductIds.Contains(oi.ProductId))
                            .Sum(oi => oi.Price * oi.Quantity)
                    })
                    .OrderBy(d => (int)Enum.Parse<DayOfWeek>(d.day))
                    .ToList();

                return Json(new { success = true, data });
            }
            else if (type == "hourly")
            {
                var data = ordersWithSellerProducts
                    .GroupBy(o => o.OrderDate.Hour)
                    .Select(g => new
                    {
                        hour = g.Key,
                        timeRange = $"{g.Key:D2}:00",
                        orders = g.Count(),
                        revenue = g.SelectMany(o => o.Items)
                            .Where(oi => sellerProductIds.Contains(oi.ProductId))
                            .Sum(oi => oi.Price * oi.Quantity)
                    })
                    .OrderBy(h => h.hour)
                    .ToList();

                return Json(new { success = true, data });
            }

            return Json(new { success = false, message = "Invalid chart type" });
        }
    }
}
