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

        [Route("/Themes")]
        public async Task<IActionResult> Themes()
        {
            var themes = await _themeService.GetInstalledThemesAsync();
            return View(themes);
        }

        [HttpPost]
        public async Task<IActionResult> ActivateTheme(string themeName)
        {
            if (string.IsNullOrEmpty(themeName))
            {
                return BadRequest();
            }

            await _themeService.ActivateThemeAsync(themeName);
            return RedirectToAction("Themes");
        }
    }
}
