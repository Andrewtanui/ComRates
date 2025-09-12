using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TanuiApp.Data;
using TanuiApp.Models;
using TanuiApp.ViewModels;
using TanuiApp.Services;
using System.Linq;
using System.Threading.Tasks;

namespace TanuiApp.Controllers
{
    public class ProductsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ProductsController> _logger;
        private readonly UserManager<Users> _userManager;
        private readonly IImageStorageService _imageStorageService;

        public ProductsController(AppDbContext context, ILogger<ProductsController> logger, UserManager<Users> userManager, IImageStorageService imageStorageService)
        {
            _context = context;
            _logger = logger;
            _userManager = userManager;
            _imageStorageService = imageStorageService;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index(string searchQuery)
        {
            var products = from p in _context.Products.Include(p => p.User)
                           select p;

            if (!string.IsNullOrEmpty(searchQuery))
            {
                products = products.Where(p =>
                    p.Name.Contains(searchQuery) ||
                    p.Description.Contains(searchQuery) ||
                    p.Category.Contains(searchQuery));
            }

            return View(await products.AsNoTracking().ToListAsync());
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> Search(string query)
        {
            var products = _context.Products
                .Include(p => p.User)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(query))
            {
                products = products.Where(p =>
                    p.Name.Contains(query) ||
                    p.Description!.Contains(query) ||
                    p.Category.Contains(query));
            }

            ViewBag.SearchQuery = query;
            var list = await products.AsNoTracking().ToListAsync();
            return View("List", list);
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> Category(string? category, string? name)
        {
            // Support both asp-route-category and asp-route-name
            var selected = (!string.IsNullOrWhiteSpace(category) ? category : name)?.Trim();
            if (string.IsNullOrWhiteSpace(selected))
            {
                return RedirectToAction(nameof(Index));
            }

            var selectedLower = selected.ToLower();

            var products = await _context.Products
                .Include(p => p.User)
                .Where(p => p.Category.ToLower() == selectedLower)
                .AsNoTracking()
                .ToListAsync();

            ViewBag.CategorySelected = selected;
            return View("List", products);
        }

        [AllowAnonymous]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var product = await _context.Products
                    .Include(p => p.User)       // seller
                    .Include(p => p.Comments)   // comments
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (product == null)
                    return NotFound();

                var reviews = await _context.Reviews
                    .AsNoTracking()
                    .Where(r => r.ProductId == id)
                    .OrderByDescending(r => r.CreatedAt)
                    .ToListAsync();

                ViewBag.Reviews = reviews;
                return View(product);
            }
            catch
            {
                return NotFound();
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddReview(int productId, int rating, string content)
        {
            if (string.IsNullOrWhiteSpace(content) || rating < 1 || rating > 5)
            {
                TempData["Error"] = "Invalid review submission.";
                return RedirectToAction("Details", new { id = productId });
            }

            var review = new Review
            {
                ProductId = productId,
                Rating = rating,
                Content = content,
                UserId = User?.Identity?.Name,
                CreatedAt = DateTime.Now
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            await UpdateProductRating(productId);

            return RedirectToAction("Details", new { id = productId });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteReview(int id)
        {
            var review = await _context.Reviews
                .Include(r => r.Product)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (review == null)
                return Json(new { success = false, message = "Review not found." });

            var currentUserId = _userManager.GetUserId(User);

            // Author or Seller
            bool isAuthor = review.UserId == currentUserId;
            bool isSeller = review.Product.UserId == currentUserId;

            if (!isAuthor && !isSeller)
                return Json(new { success = false, message = "Unauthorized." });

            _context.Reviews.Remove(review);
            await _context.SaveChangesAsync();

            await UpdateProductRating(review.ProductId);

            return Json(new { success = true, reviewId = review.Id });
        }

        [Authorize]
        public IActionResult Create()
        {
            return View(new ProductCreateViewModel());
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductCreateViewModel vm)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var imageUrl = await _imageStorageService.SaveProductImageAsync(vm.ImageFile);

                    var product = new Product
                    {
                        Name = vm.Name,
                        Description = vm.Description,
                        Price = vm.Price,
                        Category = vm.Category,
                        ImageUrl = imageUrl,
                        UserId = _userManager.GetUserId(User),
                        CreatedAt = DateTime.Now
                    };

                    _context.Products.Add(product);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Product created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating product");
                    ModelState.AddModelError(string.Empty, "An error occurred while creating the product. Please try again.");
                }
            }

            return View(vm);
        }

        private async Task UpdateProductRating(int productId)
        {
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == productId);
            if (product == null) return;

            var ratings = await _context.Reviews
                .Where(r => r.ProductId == productId)
                .Select(r => (double?)r.Rating)
                .ToListAsync();

            var average = ratings.Any() && ratings.Average().HasValue ? ratings.Average()!.Value : 0.0;
            product.Rating = Math.Round(average, 2);
            await _context.SaveChangesAsync();
        }

    }
}


