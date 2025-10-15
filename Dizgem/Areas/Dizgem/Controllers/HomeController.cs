using Dizgem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dizgem.Areas.Dizgem.Controllers
{
    [Area("Dizgem")]
    [Authorize]
    public class HomeController : Controller
    {
        private readonly IThemeService _themeService;

        public HomeController(IThemeService themeService)
        {
            _themeService = themeService;
        }

        public IActionResult Index()
        {
            return View();
        }
    }
}
