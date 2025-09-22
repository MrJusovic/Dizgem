using Dizgem.Models;
using Dizgem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dizgem.Areas.Dizgem.Controllers
{
    [Area("Dizgem")]
    [Authorize] // Sadece yetkili kullanıcılar erişebilir
    public class SettingsController : Controller
    {
        private readonly ISettingsService _settingsService;

        public SettingsController(ISettingsService settingsService)
        {
            _settingsService = settingsService;
        }

        // GET: /DizgemAdmin/Settings
        public IActionResult Index()
        {
            var currentSettings = _settingsService.Current;
            return View(currentSettings);
        }

        // POST: /DizgemAdmin/Settings
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(SettingsViewModel model)
        {
            if (ModelState.IsValid)
            {
                await _settingsService.SaveSettingsAsync(model);
                // Başarı mesajı
                TempData["SuccessMessage"] = "Ayarlar başarıyla güncellendi.";
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }
    }
}
