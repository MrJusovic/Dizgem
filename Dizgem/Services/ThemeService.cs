using Dizgem.Data;
using Dizgem.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.IO.Compression;
using System.Text.Json;

namespace Dizgem.Services
{
    public class ThemeService : IThemeService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly IConnectionStringProvider _connectionStringProvider;
        private readonly IMemoryCache _memoryCache; //  Önbellek servisi
        private const string ActiveThemeCacheKey = "ActiveThemeName";

        public ThemeService(
            ApplicationDbContext dbContext,
            IWebHostEnvironment webHostEnvironment,
            IConnectionStringProvider connectionStringProvider, IMemoryCache memoryCache)
        {
            _dbContext = dbContext;
            _hostingEnvironment = webHostEnvironment;
            _connectionStringProvider = connectionStringProvider;
            _memoryCache = memoryCache;
        }

        public async Task<List<ThemeViewModel>> GetInstalledThemesAsync()
        {
            var themesPath = Path.Combine(_hostingEnvironment.ContentRootPath, "Themes");
            if (!Directory.Exists(themesPath))
            {
                return await Task.FromResult(new List<ThemeViewModel>());
            }

            //var themes = Directory.GetDirectories(themesPath)
            //                      .Select(dir => new Theme
            //                      {
            //                          Name = Path.GetFileName(dir),
            //                          DisplayName = Path.GetFileName(dir), // Şimdilik dosya adını gösterim adı olarak kullan
            //                          IsActive = false // Varsayılan olarak pasif
            //                      })
            //                      .ToList();
            var themes = new List<ThemeViewModel>();

            // Aktif temayı kontrol et
            var activeTheme = await GetActiveThemeNameAsync();

            foreach (var themeDir in Directory.GetDirectories(themesPath))
            {
                var dirInfo = new DirectoryInfo(themeDir);
                var themeJsonPath = Path.Combine(themeDir, "theme.json");
                ThemeViewModel themeModel;

                if (File.Exists(themeJsonPath))
                {
                    // theme.json varsa, bilgileri oradan oku
                    var jsonContent = await File.ReadAllTextAsync(themeJsonPath);
                    themeModel = JsonSerializer.Deserialize<ThemeViewModel>(jsonContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new ThemeViewModel();
                }
                else
                {
                    // theme.json yoksa, varsayılan bilgileri oluştur
                    themeModel = new ThemeViewModel
                    {
                        Name = dirInfo.Name,
                        DisplayName = dirInfo.Name,
                        Version = "1.0",
                        Author = "Bilinmiyor"
                    };
                }

                themeModel.DirectoryName = dirInfo.Name;
                themeModel.IsActive = (dirInfo.Name == activeTheme);

                // Ekran görüntüsünü kontrol et
                var screenshotPath = Path.Combine(themeDir, "screenshot.png");
                if (File.Exists(screenshotPath))
                {
                    themeModel.ScreenshotUrl = $"/Themes/{themeModel.DirectoryName}/screenshot.png";
                }
                else if (string.IsNullOrEmpty(themeModel.ScreenshotUrl))
                {
                    // Varsayılan bir placeholder resim yolu
                    themeModel.ScreenshotUrl = "https://placehold.co/600x400/eeeeee/cccccc?text=Gorsel+Yok";
                }

                themes.Add(themeModel);
            }

            return await Task.FromResult(themes);
        }

        public async Task ActivateThemeAsync(string themeName)
        {
            var existingTheme = await _dbContext.Settings.FirstOrDefaultAsync(s => s.Key == "ActiveTheme");

            if (existingTheme != null)
            {
                existingTheme.Value = themeName;
            }
            else
            {
                _dbContext.Settings.Add(new Settings { Key = "ActiveTheme", Value = themeName });
            }
            await _dbContext.SaveChangesAsync();

            _memoryCache.Remove(ActiveThemeCacheKey);
        }

        public async Task<string> GetActiveThemeNameAsync()
        {
            if (_memoryCache.TryGetValue(ActiveThemeCacheKey, out string activeThemeName))
            {
                return activeThemeName; // Önbellekte varsa, doğrudan döndür.
            }

            if (string.IsNullOrWhiteSpace(_connectionStringProvider.Current))
            {
                return "Default"; // Kurulum aşamasında varsayılan temayı döndür.
            }

            var activeTheme = await _dbContext.Settings.FirstOrDefaultAsync(s => s.Key == "ActiveTheme");

            // Veritabanından okunan güncel bilgiyi, bir sonraki istekte hızlıca erişmek için önbelleğe kaydet.
            _memoryCache.Set(ActiveThemeCacheKey, activeTheme?.Value ?? "Default", System.TimeSpan.FromHours(1));

            return activeTheme?.Value ?? "Default"; // Varsayılan bir tema adı döndür
        }

        public async Task<(bool Success, string Message)> InstallThemeAsync(IFormFile themeZipFile)
        {
            if (themeZipFile == null || themeZipFile.Length == 0)
                return (false, "Lütfen bir dosya seçin.");

            if (Path.GetExtension(themeZipFile.FileName).ToLowerInvariant() != ".zip")
                return (false, "Lütfen geçerli bir .zip dosyası yükleyin.");

            var themesRootPath = Path.Combine(_hostingEnvironment.ContentRootPath, "Themes");
            var tempExtractPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            try
            {
                Directory.CreateDirectory(tempExtractPath);

                using (var stream = themeZipFile.OpenReadStream())
                {
                    using (var archive = new ZipArchive(stream))
                    {
                        archive.ExtractToDirectory(tempExtractPath, true);
                    }
                }

                var extractedDirectories = Directory.GetDirectories(tempExtractPath);
                string themeSourcePath;

                if (extractedDirectories.Length == 1 && File.Exists(Path.Combine(extractedDirectories[0], "theme.json")))
                {
                    themeSourcePath = extractedDirectories[0];
                }
                else if (File.Exists(Path.Combine(tempExtractPath, "theme.json")))
                {
                    themeSourcePath = tempExtractPath;
                }
                else
                {
                    return (false, "Yüklenen .zip dosyası geçerli bir tema yapısı içermiyor (theme.json bulunamadı).");
                }

                var themeName = new DirectoryInfo(themeSourcePath).Name;
                var finalThemePath = Path.Combine(themesRootPath, themeName);

                if (Directory.Exists(finalThemePath))
                {
                    return (false, $"'{themeName}' adında bir tema zaten mevcut.");
                }

                Directory.Move(themeSourcePath, finalThemePath);

                return (true, $"'{themeName}' teması başarıyla yüklendi.");
            }
            catch (Exception ex)
            {
                return (false, $"Tema yüklenirken bir hata oluştu: {ex.Message}");
            }
            finally
            {
                if (Directory.Exists(tempExtractPath))
                {
                    Directory.Delete(tempExtractPath, true);
                }
            }
        }

        public async Task<(bool Success, string Message)> DeleteThemeAsync(string themeName)
        {
            if (string.IsNullOrWhiteSpace(themeName))
                return (false, "Geçersiz tema adı.");

            var activeThemeName = await GetActiveThemeNameAsync();
            if (string.Equals(activeThemeName, themeName, StringComparison.OrdinalIgnoreCase))
            {
                return (false, "Aktif olan bir tema silinemez.");
            }

            // "Default" temanın silinmesini engellemek iyi bir güvenlik önlemidir.
            if (string.Equals(themeName, "Default", StringComparison.OrdinalIgnoreCase))
            {
                return (false, "Varsayılan tema silinemez.");
            }

            var themePath = Path.Combine(_hostingEnvironment.ContentRootPath, "Themes", themeName);

            if (!Directory.Exists(themePath))
            {
                return (false, "Silinecek tema klasörü bulunamadı.");
            }

            try
            {
                Directory.Delete(themePath, true);
                return (true, $"'{themeName}' teması başarıyla silindi.");
            }
            catch (Exception ex)
            {
                return (false, $"Tema silinirken bir hata oluştu: {ex.Message}");
            }
        }
    }
}
