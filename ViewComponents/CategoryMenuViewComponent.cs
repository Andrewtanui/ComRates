using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
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

        public async Task<IViewComponentResult> InvokeAsync()
        {
            try
            {
                var categories = await _context.Products
                    .Select(p => p.Category)
                    .Where(c => c != null && c != "")
                    .Distinct()
                    .OrderBy(c => c)
                    .ToListAsync();

                return View(categories);
            }
            catch
            {
                return View(new List<string>());
            }
        }
    }
}
