using Dizgem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dizgem.Areas.Dizgem.Controllers
{
    [Area("Dizgem")]
    [Authorize(Roles = "Admin")]
    public class UpdateController : Controller
    {
        private readonly IUpdateService _updateService;
        private readonly IHostApplicationLifetime _appLifetime;

        public UpdateController(IUpdateService updateService, IHostApplicationLifetime appLifetime)
        {
            _updateService = updateService;
            _appLifetime = appLifetime;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CheckForUpdate()
        {
            var model = await _updateService.CheckForUpdateAsync();
            return PartialView("_UpdateStatusPartial", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StartUpdate(string downloadUrl)
        {
            if (string.IsNullOrWhiteSpace(downloadUrl))
            {
                return BadRequest("İndirme linki geçersiz.");
            }

            await _updateService.DownloadAndPrepareUpdateAsync(downloadUrl);

            // Uygulamayı nazikçe kapat. Hosting ortamı (IIS vb.) yeniden başlatacaktır.
            _appLifetime.StopApplication();

            return Ok(new { message = "Güncelleme indirildi. Uygulama yeniden başlatılıyor..." });
        }
    }
}
