using Dizgem.Data;
using Dizgem.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System.Security.Claims;
using System.Text.Json;

namespace Dizgem.Services
{
    public class MediaService : IMediaService
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly UserManager<User> _userManager;
        private readonly IHttpContextAccessor _httpContextAccessor;

        // Sitenizde kullanılacak standart görsel boyutlarını burada tanımlayın
        private readonly Dictionary<string, int> _imageSizes = new Dictionary<string, int>
        {
            { "thumbnail", 150 },
            { "light", 300 },
            { "medium", 768 },
            { "large", 1024 },
            { "@2x", 2048 }
            // "full" boyutu orijinal dosyadır
        };

        public MediaService(
            ApplicationDbContext context,
            IWebHostEnvironment webHostEnvironment,
            UserManager<User> userManager,
            IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _userManager = userManager;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<Media> UploadFileAsync(IFormFile file, Guid? uploaderUserId)
        {
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("Yüklenecek dosya boş olamaz.");
            }

            // 1. Dosya yolu ve adını oluştur
            var originalFileName = Path.GetFileName(file.FileName);
            var uniqueFileNameBase = Guid.NewGuid().ToString("N"); // Benzersiz bir temel ad
            var fileExtension = Path.GetExtension(originalFileName);
            var uniqueFileName = uniqueFileNameBase + fileExtension;

            var relativeUploadPath = Path.Combine("uploads", DateTime.Now.ToString("yyyy"), DateTime.Now.ToString("MM"));
            var absoluteUploadDir = Path.Combine(_webHostEnvironment.WebRootPath, relativeUploadPath);
            Directory.CreateDirectory(absoluteUploadDir); // Klasör yoksa oluştur

            var absoluteFilePath = Path.Combine(absoluteUploadDir, uniqueFileName);
            var relativeFilePath = Path.Combine("/", relativeUploadPath, uniqueFileName).Replace("\\", "/");

            // 2. Orijinal dosyayı sunucuya kaydet
            await using (var stream = new FileStream(absoluteFilePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
            
            string determinedContentType;
            var provider = new FileExtensionContentTypeProvider();

            if (!provider.Mappings.ContainsKey(".psd"))
            {
                // GetIconForFileType fonksiyonu "photoshop" arayacak
                provider.Mappings.Add(".psd", "application/photoshop");
            }
            if (!provider.Mappings.ContainsKey(".ai"))
            {
                // GetIconForFileType fonksiyonu "illustrator" arayacak
                provider.Mappings.Add(".ai", "application/illustrator");
            }
            if (!provider.Mappings.ContainsKey(".indd"))
            {
                // GetIconForFileType fonksiyonu "indesign" arayacak
                provider.Mappings.Add(".indd", "application/indesign");
            }
            if (!provider.Mappings.ContainsKey(".prproj"))
            {
                // GetIconForFileType fonksiyonu "premiere" arayacak
                provider.Mappings.Add(".prproj", "application/premiere-pro-project");
            }
            if (!provider.Mappings.ContainsKey(".aep"))
            {
                // GetIconForFileType fonksiyonu "aftereffects" arayacak
                provider.Mappings.Add(".aep", "application/aftereffects-project");
            }
            if (!provider.Mappings.ContainsKey(".apk"))
            {
                // GetIconForFileType fonksiyonu "aftereffects" arayacak
                provider.Mappings.Add(".apk", "application/vnd.android.package-archive");
            }
            // --- Ekleme Sonu ---



            // FileExtensionContentTypeProvider'a dosya adını (veya sadece uzantıyı) veriyoruz.
            // Bilinen bir uzantıysa (örn: .png, .pdf, .docx), bize doğru MIME tipini (out determinedContentType) verecek.
            if (!provider.TryGetContentType(originalFileName, out determinedContentType))
            {
                // Eğer provider bu uzantıyı bilmiyorsa (örn: ".benimozeluzantim"),
                // o zaman tarayıcının gönderdiği (muhtemelen "application/octet-stream" olan)
                // orijinal değere geri dönüyoruz.
                determinedContentType = file.ContentType ?? "application/octet-stream";
            }

            // 3. Medya nesnesini oluştur
            var media = new Media
            {
                Id = Guid.NewGuid(),
                FileName = originalFileName,
                FileType = determinedContentType,
                FileSize = file.Length,
                UrlFull = relativeFilePath,
                UploaderUserId = uploaderUserId ?? GetCurrentUserId(),
                Title = Path.GetFileNameWithoutExtension(originalFileName), // Varsayılan başlık
                AltText = Path.GetFileNameWithoutExtension(originalFileName), // Varsayılan alt metin
                Description = "",
                UploadedAt = DateTime.Now,
                ImageSizesJson = "{}"
            };

            // 4. Eğer bir görselse, yeniden boyutlandır
            if (file.ContentType.StartsWith("image/"))
            {
                try
                {
                    var generatedSizes = await GenerateImageSizesAsync(absoluteFilePath, uniqueFileNameBase, fileExtension, relativeUploadPath);
                    media.ImageSizesJson = JsonSerializer.Serialize(generatedSizes);
                }
                catch (Exception ex)
                {
                    // Görsel işlenemezse bile (örn: bozuk dosya), orijinal dosyayı kaydetmeye devam et.
                    // Hata loglanabilir.
                    media.ImageSizesJson = "{}";
                }
            }

            // 5. Veritabanına kaydet
            _context.Media.Add(media);
            await _context.SaveChangesAsync();

            return media;
        }

        public async Task<MediaListViewModel> GetMediaListAsync(int page, int pageSize, string searchQuery, string fileType)
        {
            var query = _context.Media.AsNoTracking();

            // Filtreleme
            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                query = query.Where(m => m.FileName.Contains(searchQuery) || m.Title.Contains(searchQuery) || m.AltText.Contains(searchQuery));
            }
            if (!string.IsNullOrWhiteSpace(fileType) && fileType != "all")
            {
                query = query.Where(m => m.FileType.StartsWith(fileType)); // "image", "application" vb.
            }

            // Sayfalama
            var totalCount = await query.CountAsync();
            var mediaItems = await query.OrderByDescending(m => m.UploadedAt)
                                        .Skip((page - 1) * pageSize)
                                        .Take(pageSize)
                                        .ToListAsync();

            // ViewModel'a dönüştür
            var viewModels = mediaItems.Select(m => new MediaSummaryViewModel
            {
                Id = m.Id,
                FileName = m.FileName,
                FileType = m.FileType,
                Title = m.Title,
                UrlFull = m.UrlFull,
                ThumbnailUrl = m.FileType.StartsWith("image/")
                               ? (m.ImageSizes.ContainsKey("thumbnail") ? m.ImageSizes["thumbnail"] : m.UrlFull)
                               : GetIconForFileType(m.FileType) // Görsel değilse bir ikon yolu döndür
            }).ToList();

            return new MediaListViewModel
            {
                MediaItems = viewModels,
                CurrentPage = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                SearchQuery = searchQuery,
                FileType = fileType
            };
        }

        public async Task<MediaDetailViewModel> GetMediaByIdAsync(Guid id)
        {
            var media = await _context.Media.AsNoTracking()
                .Include(m => m.Uploader)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (media == null) return null;

            return new MediaDetailViewModel
            {
                Id = media.Id,
                FileName = media.FileName,
                FileType = media.FileType,
                FileSize = media.FileSize,
                UploadedAt = media.UploadedAt,
                UploaderName = media.Uploader?.DisplayName ?? "Bilinmiyor",
                Title = media.Title,
                Description = media.Description,
                AltText = media.AltText,
                UrlFull = media.UrlFull,
                ImageSizes = media.ImageSizes
            };
        }

        public async Task<(bool Success, string Message)> UpdateMetadataAsync(Guid id, string title, string description, string altText)
        {
            var media = await _context.Media.FindAsync(id);
            if (media == null)
            {
                return (false, "Dosya bulunamadı.");
            }

            media.Title = title;
            media.Description = description;
            media.AltText = altText;

            _context.Media.Update(media);
            await _context.SaveChangesAsync();

            return (true, "Medya bilgileri başarıyla güncellendi.");
        }

        public async Task<(bool Success, string Message)> DeleteFileAsync(Guid id)
        {
            var media = await _context.Media.FindAsync(id);
            if (media == null)
            {
                return (false, "Silinecek dosya bulunamadı.");
            }

            // 1. Sunucudaki tüm dosyaları sil (orijinal + tüm boyutlar)
            var filesToDelete = new List<string> { media.UrlFull };
            if (media.ImageSizes != null)
            {
                filesToDelete.AddRange(media.ImageSizes.Values);
            }

            foreach (var filePath in filesToDelete.Where(p => !string.IsNullOrEmpty(p)))
            {
                var absolutePath = Path.Combine(_webHostEnvironment.WebRootPath, filePath.TrimStart('/'));
                if (File.Exists(absolutePath))
                {
                    File.Delete(absolutePath);
                }
            }

            // 2. Veritabanından kaydı sil
            _context.Media.Remove(media);
            await _context.SaveChangesAsync();

            return (true, $"'{media.FileName}' başarıyla silindi.");
        }

        public async Task<(bool Success, string Message)> RegenerateThumbnailsAsync(Guid id)
        {
            var media = await _context.Media.FindAsync(id);
            if (media == null)
            {
                return (false, "Dosya bulunamadı.");
            }
            if (!media.FileType.StartsWith("image/"))
            {
                return (false, "Bu dosya bir görsel olmadığı için yeniden boyutlandırılamaz.");
            }

            var originalFilePath = Path.Combine(_webHostEnvironment.WebRootPath, media.UrlFull.TrimStart('/'));
            if (!File.Exists(originalFilePath))
            {
                return (false, "Orijinal dosya sunucuda bulunamadı. İşlem iptal edildi.");
            }

            // 1. Eski oluşturulmuş boyutları sil
            foreach (var oldSizeUrl in media.ImageSizes.Values)
            {
                var oldSizePath = Path.Combine(_webHostEnvironment.WebRootPath, oldSizeUrl.TrimStart('/'));
                if (File.Exists(oldSizePath))
                {
                    File.Delete(oldSizePath);
                }
            }

            // 2. Yeniden boyutlandırma işlemini çalıştır
            try
            {
                var uniqueFileNameBase = Path.GetFileNameWithoutExtension(media.UrlFull);
                var fileExtension = Path.GetExtension(media.UrlFull);
                var relativeUploadPath = Path.GetDirectoryName(media.UrlFull).Replace("\\", "/").TrimStart('/');

                var newSizes = await GenerateImageSizesAsync(originalFilePath, uniqueFileNameBase, fileExtension, relativeUploadPath);
                media.ImageSizesJson = JsonSerializer.Serialize(newSizes);

                _context.Media.Update(media);
                await _context.SaveChangesAsync();

                return (true, "Görsel boyutları başarıyla yeniden oluşturuldu.");
            }
            catch (Exception ex)
            {
                return (false, $"Görseller oluşturulurken bir hata oluştu: {ex.Message}");
            }
        }

        // --- YARDIMCI METOTLAR ---

        /// <summary>
        /// Orijinal bir görsel dosyasından standart boyutları oluşturan özel metot.
        /// </summary>
        private async Task<Dictionary<string, string>> GenerateImageSizesAsync(string originalAbsoluteFilePath, string uniqueFileNameBase, string fileExtension, string relativeUploadPath)
        {
            var generatedSizes = new Dictionary<string, string>();

            using (var image = await Image.LoadAsync(originalAbsoluteFilePath))
            {
                var originalWidth = image.Width;

                foreach (var size in _imageSizes)
                {
                    var sizeName = size.Key;
                    var newWidth = size.Value;

                    // Eğer orijinal görsel, hedeflenen boyuttan küçükse bu boyutu oluşturma
                    if (originalWidth <= newWidth)
                    {
                        continue;
                    }

                    var newHeight = (int)((double)newWidth / image.Width * image.Height);
                    var sizeFileName = $"{uniqueFileNameBase}-{sizeName}{fileExtension}";
                    var sizeAbsolutePath = Path.Combine(_webHostEnvironment.WebRootPath, relativeUploadPath, sizeFileName);
                    var relativeSizePath = Path.Combine("/", relativeUploadPath, sizeFileName).Replace("\\", "/");

                    // Görseli klonlayarak yeniden boyutlandır ve kaydet
                    using (var clonedImage = image.Clone(x => x.Resize(newWidth, newHeight, KnownResamplers.Lanczos3)))
                    {
                        await clonedImage.SaveAsync(sizeAbsolutePath); // Dosyayı kaydet
                    }
                    generatedSizes.Add(sizeName, relativeSizePath);
                }
            }
            return generatedSizes;
        }

        /// <summary>
        /// Dosya türüne (MIME tipi veya uzantı) göre ikon yolu döndürür.
        /// Microsoft Office, OpenOffice/LibreOffice ve Google Workspace formatlarını destekler.
        /// </summary>
        /// <param name="fileType">Dosya türü bilgisi (örn: "application/pdf", ".docx", "vnd.google-apps.document")</param>
        /// <returns>İkonun sunucu üzerindeki yolu</returns>
        private string GetIconForFileType(string fileType)
        {
            // Güvenlik kontrolü: fileType null veya boş ise varsayılan ikonu döndür.
            if (string.IsNullOrEmpty(fileType))
            {
                return "/icons/text.png";
            }

            // Karşılaştırmayı büyük/küçük harfe duyarsız hale getir.
            string normalizedType = fileType.ToLower();

            switch (true)
            {
                // --- E-Tablo Dosyaları (Excel, Calc, Google Sheet) ---
                // !!! ÖNEMLİ: "spreadsheet" KONTROLÜ "document" KONTROLÜNDEN ÖNCE GELMELİDİR.
                case bool _ when normalizedType.Contains("spreadsheet") ||
                                 normalizedType.Contains("excel") ||
                                 normalizedType.Contains(".ods"):
                    return "/icons/xls.png"; // veya excel.png

                // --- Sunum Dosyaları (PowerPoint, Impress, Google Slides) ---
                // !!! ÖNEMLİ: "presentation" KONTROLÜ "document" KONTROLÜNDEN ÖNCE GELMELİDİR.
                case bool _ when normalizedType.Contains("presentation") ||
                                 normalizedType.Contains("powerpoint") ||
                                 normalizedType.Contains(".odp"):
                    return "/icons/ppt.png"; // veya powerpoint.png

                // --- Doküman Dosyaları (Word, Writer, Google Doc) ---
                // Bu kontrol, spreadsheet ve presentation'dan sonra gelmelidir.
                case bool _ when normalizedType.Contains("document") ||
                                 normalizedType.Contains("word") ||
                                 normalizedType.Contains(".odt"):
                    return "/icons/doc.png"; // veya word.png

                // --- PDF ---
                case bool _ when normalizedType.Contains("pdf"):
                    return "/icons/pdf.png";

                // --- Arşiv Dosyaları ---
                case bool _ when normalizedType.Contains("zip") ||
                                 normalizedType.Contains("archive") ||
                                 normalizedType.Contains("rar") ||
                                 normalizedType.Contains("7z") ||
                                 normalizedType.Contains("x-compressed"):
                    return "/icons/zip.png";

                // --- Medya Dosyaları ---
                case bool _ when normalizedType.Contains("video"):
                    return "/icons/video.png";
                case bool _ when normalizedType.Contains("audio") ||
                                 normalizedType.Contains("mp3") ||
                                 normalizedType.Contains("wav"):
                    return "/icons/record.png";

                // --- Tasarım Dosyaları ---
                case bool _ when normalizedType.Contains("photoshop") || // Adobe Photoshop
                         normalizedType.Contains(".psd"):
                    return "/icons/psd.png";
                case bool _ when normalizedType.Contains("illustrator") || // Adobe Illustrator
                         normalizedType.Contains(".ai") ||
                         normalizedType.Contains("postscript"):
                    return "/icons/ai.png"; // (Illustrator ikonu)
                case bool _ when normalizedType.Contains("indesign") || // Adobe InDesign
                         normalizedType.Contains(".indd"):
                    return "/icons/indd.png"; // (InDesign ikonu)
                case bool _ when normalizedType.Contains("image") || // Genel resim MIME türleri
                                 normalizedType.Contains(".jpg") ||
                                 normalizedType.Contains(".jpeg") ||
                                 normalizedType.Contains(".png") ||
                                 normalizedType.Contains(".gif"):
                    return "/icons/image.png";

                // --- Kod ve Veri Dosyaları ---
                case bool _ when normalizedType.Contains("php"):
                    return "/icons/php.png";
                case bool _ when normalizedType.Contains("sql"):
                    return "/icons/sql.png";
                case bool _ when normalizedType.Contains("text") || normalizedType.Contains("txt"):
                    return "/icons/txt.png";
                case bool _ when normalizedType.Contains("html") || normalizedType.Contains("css") || normalizedType.Contains("js"):
                    return "/icons/code.png";
                case bool _ when normalizedType.Contains("xml"): return "/icons/xml.png";
                case bool _ when normalizedType.Contains("vnd.android.package-archive"): return "/icons/apk.png";
                // --- Diğer tüm durumlar için varsayılan ikon ---
                default:
                    return "/icons/text.png";
            }
        }

        /// <summary>
        /// Aktif HTTP isteğinden o an giriş yapmış kullanıcının ID'sini alır.
        /// </summary>
        private Guid? GetCurrentUserId()
        {
            var userIdString = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (Guid.TryParse(userIdString, out Guid userId))
            {
                return userId;
            }
            return null;
        }
    }
}
