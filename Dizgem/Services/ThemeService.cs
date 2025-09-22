using Dizgem.Data;
using Dizgem.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;

namespace Dizgem.Services
{
    public class ThemeService : IThemeService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly IConnectionStringProvider _connectionStringProvider;

        public ThemeService(
            ApplicationDbContext dbContext,
            IWebHostEnvironment webHostEnvironment,
            IConnectionStringProvider connectionStringProvider)
        {
            _dbContext = dbContext;
            _hostingEnvironment = webHostEnvironment;
            _connectionStringProvider = connectionStringProvider;
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
        }

        public async Task<string> GetActiveThemeNameAsync()
        {
            if (string.IsNullOrWhiteSpace(_connectionStringProvider.Current))
            {
                return "Default"; // Kurulum aşamasında varsayılan temayı döndür.
            }

            var activeTheme = await _dbContext.Settings.FirstOrDefaultAsync(s => s.Key == "ActiveTheme");
            return activeTheme?.Value ?? "Default"; // Varsayılan bir tema adı döndür
        }
    }
}
