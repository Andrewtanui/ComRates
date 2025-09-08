using System.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TanuiApp.Models;

namespace TanuiApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly UserManager<Users> _userManager;

        public HomeController(ILogger<HomeController> logger, UserManager<Users> userManager)
        {
            _logger = logger;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var currentUser = await _userManager.GetUserAsync(User);
                ViewBag.FullName = currentUser?.FullName;
            }

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
