using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Dizgem.Controllers
{

    // Editor.js'in FetchUrl isteğinde göndereceği JSON'u karşılamak için bir model
    public class FetchUrlRequest
    {
        [Required]
        [Url]
        public string Url { get; set; }
    }

    [ApiController] // Bu attribute, sınıfın bir API controller olduğunu belirtir ve bazı standart davranışları etkinleştirir.
    [Route("api/[controller]")] // Bu endpoint'e /api/upload adresinden erişilecek.
    [Authorize] // Bu endpoint'i sadece giriş yapmış kullanıcılar kullanabilir.
    public class UploadController : ControllerBase
    {
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IHttpClientFactory _httpClientFactory;

        public UploadController(IWebHostEnvironment webHostEnvironment, IHttpClientFactory httpClientFactory)
        {
            _webHostEnvironment = webHostEnvironment;
            _httpClientFactory = httpClientFactory;
        }

        [HttpPost("UploadImage")] // Bu metoda /api/upload/UploadImage adresinden POST isteği ile erişilir.
        public async Task<IActionResult> UploadImage(IFormFile image)
        {
            // --- Güvenlik ve Doğrulama Kontrolleri ---
            if (image == null || image.Length == 0)
            {
                return BadRequest(new { success = 0, message = "Lütfen bir dosya seçin." });
            }

            // Sadece belirli resim türlerine izin ver
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".pdf", ".doc", ".docx", ".ppt", ".pptx", ".xls", ".xlsx" };
            var extension = Path.GetExtension(image.FileName).ToLowerInvariant();
            if (string.IsNullOrEmpty(extension) || !allowedExtensions.Contains(extension))
            {
                return BadRequest(new { success = 0, message = "Geçersiz dosya türü. Sadece .jpg, .png, .gif, .webp, .pdf, .doc, .docx, .ppt, .pptx, .xls, .xlsx kabul edilir." });
            }

            // Dosya boyutunu sınırla (örneğin 5 MB)
            long maxFileSize = 5 * 1024 * 1024;
            if (image.Length > maxFileSize)
            {
                return BadRequest(new { success = 0, message = "Dosya boyutu 5 MB'den büyük olamaz." });
            }


            // --- Dosyayı Sunucuya Kaydetme ---
            try
            {
                // Yüklenen dosyaların kaydedileceği klasör: wwwroot/uploads/images/YIL/AY/
                // Bu yapı, on binlerce dosyayı tek bir klasörde tutmaktan daha verimlidir.
                var year = DateTime.UtcNow.Year.ToString();
                var month = DateTime.UtcNow.Month.ToString("d2"); // 09, 10, 11 gibi
                var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "images", year, month);

                // Klasör yoksa oluştur
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // Dosya adının başka bir dosyayla çakışmaması için benzersiz bir ad oluşturuyoruz.
                var uniqueFileName = Guid.NewGuid().ToString() + "_" + image.FileName;
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                // Dosyayı diske kaydet
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await image.CopyToAsync(stream);
                }

                // --- Editor.js'in Beklediği Cevabı Hazırlama ---
                // Tarayıcının erişebileceği public URL'i oluşturuyoruz.
                var publicUrl = $"/uploads/images/{year}/{month}/{uniqueFileName}";

                // Editor.js Image Tool, bu formatta bir JSON cevabı bekler.
                var response = new
                {
                    success = 1,
                    file = new
                    {
                        url = publicUrl
                    }
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                // Hata durumunda loglama yapabilir ve sunucu hatası dönebiliriz.
                // Log.Error(ex, "Dosya yüklenirken hata oluştu.");
                return StatusCode(500, new { success = 0, message = "Dosya yüklenirken bir sunucu hatası oluştu." });
            }
        }

        [HttpPost("FetchUrl")]
        public async Task<IActionResult> FetchUrl([FromBody] FetchUrlRequest request)
        {
            // Model doğrulaması otomatik olarak çalışır ([ApiController] sayesinde)
            if (!Uri.TryCreate(request.Url, UriKind.Absolute, out var uri))
            {
                return BadRequest(new { success = 0, message = "Geçersiz URL formatı." });
            }

            try
            {
                var client = _httpClientFactory.CreateClient();
                var httpResponse = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead); // Sadece başlıkları oku

                httpResponse.EnsureSuccessStatusCode(); // 2xx dışında bir kod varsa hata fırlatır

                // --- Güvenlik Kontrolleri ---
                var contentType = httpResponse.Content.Headers.ContentType?.MediaType;
                if (contentType == null || !contentType.StartsWith("image/"))
                {
                    return BadRequest(new { success = 0, message = "Belirtilen URL bir resim dosyası değil." });
                }

                long? contentLength = httpResponse.Content.Headers.ContentLength;
                long maxFileSize = 5 * 1024 * 1024; // 5 MB
                if (contentLength.HasValue && contentLength.Value > maxFileSize)
                {
                    return BadRequest(new { success = 0, message = "Dosya boyutu 5 MB'den büyük olamaz." });
                }

                // --- Dosyayı İndirme ve Kaydetme ---
                using var stream = await httpResponse.Content.ReadAsStreamAsync();

                // Orijinal dosya adını URL'den almayı dene
                var originalFileName = Path.GetFileName(uri.LocalPath);
                if (string.IsNullOrWhiteSpace(originalFileName))
                {
                    originalFileName = "downloaded_image.jpg"; // Fallback
                }

                var (publicUrl, _) = await SaveFile(stream, originalFileName);

                return Ok(new { success = 1, file = new { url = publicUrl } });
            }
            catch (HttpRequestException)
            {
                return BadRequest(new { success = 0, message = "URL'den resim indirilemedi. URL'in erişilebilir olduğundan emin olun." });
            }
            catch (Exception)
            {
                return StatusCode(500, new { success = 0, message = "Resim işlenirken bir sunucu hatası oluştu." });
            }
        }

        // --- ORTAK DOSYA KAYDETME METODU (Kod Tekrarını Önlemek İçin) ---
        private async Task<(string PublicUrl, string FilePath)> SaveFile(Stream stream, string originalFileName)
        {
            var year = DateTime.UtcNow.Year.ToString();
            var month = DateTime.UtcNow.Month.ToString("d2");
            var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "images", year, month);

            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var uniqueFileName = Guid.NewGuid().ToString() + "_" + originalFileName;
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await stream.CopyToAsync(fileStream);
            }

            var publicUrl = $"/uploads/images/{year}/{month}/{uniqueFileName}";
            return (publicUrl, filePath);
        }
    }
}
