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
            if (string.IsNullOrEmpty(currentSettings?.SiteUrl))
            {
                currentSettings.SiteUrl = $"{Request.Scheme}://{Request.Host}";
            }
            return View(currentSettings);
        }

        // POST: /DizgemAdmin/Settings
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(SettingsViewModel model)
        {
            if (ModelState.IsValid)
            {
                if (string.IsNullOrEmpty(model?.SiteUrl))
                {
                    model.SiteUrl = $"{Request.Scheme}://{Request.Host}";
                }
                await _settingsService.SaveSettingsAsync(model);
                // Başarı mesajı
                TempData["SuccessMessage"] = "Ayarlar başarıyla güncellendi.";
                return RedirectToAction(nameof(Index));
            }
            else
            {
                TempData["ErrorMessage"] = ModelState.ToHtmlErrorList();
            }

                return View(model);
        }
    }
}
