using Dizgem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dizgem.Controllers
{
    // Bu attribute, bu kontrolcüdeki tüm aksiyonlara erişimi "Admin" rolü ile sınırlar.
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly IThemeService _themeService;
        public AdminController(IThemeService themeService)
        {
            _themeService = themeService;
        }

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

        // Yönetici panelinin ana sayfası
        public IActionResult Index()
        {
            return View();
        }
    }
}
