using Microsoft.AspNetCore.Mvc;
using System.Linq;
using TanuiApp.Data;

namespace TanuiApp.ViewComponents
{
    public class CategoryMenuViewComponent : ViewComponent
    {
        private readonly AppDbContext _context;

        public CategoryMenuViewComponent(AppDbContext context)
        {
            _context = context;
        }

        public IViewComponentResult Invoke()
        {
            var categories = _context.Products
                .Select(p => p.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToList();

            return View(categories);
        }
    }
}
