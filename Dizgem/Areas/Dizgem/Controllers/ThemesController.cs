using Dizgem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dizgem.Areas.Dizgem.Controllers
{
    [Area("Dizgem")]
    [Authorize(Roles = "Admin")]
    public class ThemesController : Controller
    {
        private readonly IThemeService _themeService;

        public ThemesController(IThemeService themeService)
        {
            _themeService = themeService;
        }

        public async Task<IActionResult> Index()
        {
            var themes = await _themeService.GetInstalledThemesAsync();
            return View(themes);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Activate(string themeName)
        {
            if (string.IsNullOrWhiteSpace(themeName))
            {
                TempData["ErrorMessage"] = "Geçersiz tema adı.";
                return RedirectToAction(nameof(Index));
            }

            await _themeService.ActivateThemeAsync(themeName);
            TempData["SuccessMessage"] = $"'{themeName}' teması başarıyla etkinleştirildi.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadTheme(IFormFile themeFile)
        {
            if (themeFile == null)
            {
                TempData["ErrorMessage"] = "Lütfen yüklenecek bir tema dosyası seçin.";
                return RedirectToAction(nameof(Index));
            }

            var (success, message) = await _themeService.InstallThemeAsync(themeFile);

            if (success)
            {
                TempData["SuccessMessage"] = message;
            }
            else
            {
                TempData["ErrorMessage"] = message;
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string themeName)
        {
            var (success, message) = await _themeService.DeleteThemeAsync(themeName);

            if (success)
            {
                TempData["SuccessMessage"] = message;
            }
            else
            {
                TempData["ErrorMessage"] = message;
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
