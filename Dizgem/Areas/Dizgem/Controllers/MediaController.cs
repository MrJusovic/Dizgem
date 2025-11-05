using Dizgem.Models;
using Dizgem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dizgem.Areas.Dizgem.Controllers
{
    [Area("Dizgem")]
    [Authorize(Roles = "Admin")]
    public class MediaController : Controller
    {
        private readonly IMediaService _mediaService;
        private const int PageSize = 24; // Sayfa başına gösterilecek medya sayısı

        public MediaController(IMediaService mediaService)
        {
            _mediaService = mediaService;
        }

        // GET: /Dizgem/Media
        public async Task<IActionResult> Index(int p = 1, string q = null, string type = "all")
        {
            if (p < 1) p = 1;
            var viewModel = await _mediaService.GetMediaListAsync(p, PageSize, q, type);
            return View(viewModel);
        }

        // POST: /Dizgem/Media/Upload
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(List<IFormFile> files)
        {
            if (files == null || files.Count == 0)
            {
                TempData["ErrorMessage"] = "Yüklenecek dosya seçilmedi.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                foreach (var file in files)
                {
                    await _mediaService.UploadFileAsync(file, null); // UserID serviste alınacak
                }
                TempData["SuccessMessage"] = $"{files.Count} adet dosya başarıyla yüklendi.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Dosya yüklenirken bir hata oluştu: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: /Dizgem/Media/Detail/{id}
        public async Task<IActionResult> Detail(Guid id)
        {
            var viewModel = await _mediaService.GetMediaByIdAsync(id);
            if (viewModel == null)
            {
                return NotFound();
            }
            return View(viewModel);
        }

        // POST: /Dizgem/Media/Update
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(MediaDetailViewModel model)
        {
            ModelState.Remove("Title");
            ModelState.Remove("AltText");
            ModelState.Remove("Description");
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Model geçerli değil.";
                return View("Detail", model); // Hata varsa Detay sayfasını tekrar göster
            }
            model.Description = string.IsNullOrEmpty(model.Description) ? "" : model.Description;
            var (success, message) = await _mediaService.UpdateMetadataAsync(model.Id, model.Title, model.Description, model.AltText);

            if (success)
            {
                TempData["SuccessMessage"] = message;
            }
            else
            {
                TempData["ErrorMessage"] = message;
            }

            return RedirectToAction(nameof(Detail), new { id = model.Id });
        }

        // POST: /Dizgem/Media/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var (success, message) = await _mediaService.DeleteFileAsync(id);
            if (success)
            {
                return Ok(new { success = true, message = message });
            }
            return BadRequest(new { success = false, message = message });
        }

        // POST: /Dizgem/Media/Regenerate
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Regenerate(Guid id)
        {
            var (success, message) = await _mediaService.RegenerateThumbnailsAsync(id);
            if (success)
            {
                return Ok(new { success = true, message = message });
            }
            return BadRequest(new { success = false, message = message });
        }

        /// <summary>
        /// Yazı/Sayfa editöründeki "Kapak Fotoğrafı Seç" modalı için resim listesini
        /// PartialView olarak döndürür. Sadece görselleri listeler.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetMediaLibraryPartial(int p = 1, string q = null)
        {
            if (p < 1) p = 1;
            // Servisi sadece "image" tipindeki dosyaları getirecek şekilde çağır
            var viewModel = await _mediaService.GetMediaListAsync(p, PageSize, q, "image");

            // Bu PartialView'ı bir sonraki adımda oluşturacağız
            return PartialView("_MediaLibraryModalList", viewModel);
        }

        /// <summary>
        /// Modal içerisinden dosya yüklemeyi yönetir ve JSON olarak sonuç döndürür.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadFromModal(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { success = false, message = "Yüklenecek dosya seçilmedi." });
            }

            // Güvenlik: Sadece görsel yüklenebildiğinden emin ol (opsiyonel ama önerilir)
            if (!file.ContentType.StartsWith("image/"))
            {
                return BadRequest(new { success = false, message = "Sadece görsel dosyaları (.jpg, .png, .gif, .webp) yükleyebilirsiniz." });
            }

            try
            {
                var media = await _mediaService.UploadFileAsync(file, null); // UserID serviste alınacak

                // Başarılı yüklemenin ardından, modal'ın hemen kullanabilmesi için
                // yeni oluşturulan medya dosyasının detaylarını JSON olarak döndür.
                return Ok(new
                {
                    success = true,
                    message = "Dosya başarıyla yüklendi.",
                    // Modal'ın seçilen resmi hemen listeye eklemesi için gerekli bilgiler
                    id = media.Id,
                    title = media.Title,
                    url = media.UrlFull,
                    thumbnailUrl = media.ImageSizes.ContainsKey("thumbnail") ? media.ImageSizes["thumbnail"] : media.UrlFull
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = $"Dosya yüklenirken bir hata oluştu: {ex.Message}" });
            }
        }
    }
}
