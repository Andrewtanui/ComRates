using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TanuiApp.Data;
using TanuiApp.Models;

namespace TanuiApp.Controllers
{
    [Authorize] // 👈 Only logged-in users can add products
    public class ProductsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<Users> _userManager;

        public ProductsController(AppDbContext context, UserManager<Users> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Show all products
        [AllowAnonymous] // Anyone can view products
        public IActionResult Index()
        {
            var products = _context.Products.ToList();
            return View(products);
        }

        // Category page
        [AllowAnonymous]
        public IActionResult Category(string name)
        {
            var products = _context.Products
                .Where(p => p.Category == name)
                .ToList();

            ViewBag.CategoryName = name;
            return View(products);
        }

        // GET: Add product form
        public IActionResult Create()
        {
            return View();
        }

        // POST: Save new product
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);

                model.UserId = user.Id; // link to logged-in user
                model.Rating = 0;       // default
                model.OnSale = false;   // default

                _context.Products.Add(model);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }
    }
}
