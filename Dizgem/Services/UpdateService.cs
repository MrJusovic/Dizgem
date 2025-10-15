using Dizgem.Models;
using System.IO.Compression;
using System.Reflection;
using System.Text.Json.Nodes;

namespace Dizgem.Services
{
    public class UpdateService : IUpdateService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IWebHostEnvironment _env;

        // --- ÖNEMLİ: BU ALANLARI KENDİ GITHUB BİLGİLERİNİZLE DEĞİŞTİRİN ---
        private const string GitHubOwner = "mrjusovic";
        private const string GitHubRepo = "Dizgem";
        // --------------------------------------------------------------------

        public UpdateService(IHttpClientFactory httpClientFactory, IWebHostEnvironment env)
        {
            _httpClientFactory = httpClientFactory;
            _env = env;
        }

        public async Task<UpdateCheckViewModel> CheckForUpdateAsync()
        {
            var result = new UpdateCheckViewModel
            {
                CurrentVersion = GetCurrentVersion()
            };

            try
            {
                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Add("User-Agent", "Dizgem-CMS-Updater");

                var url = $"https://api.github.com/repos/{GitHubOwner}/{GitHubRepo}/releases/latest";
                var response = await client.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    result.ErrorMessage = "Güncelleme bilgileri alınamadı. GitHub API ile iletişim kurulamadı.";
                    return result;
                }

                var jsonString = await response.Content.ReadAsStringAsync();
                var releaseInfo = JsonNode.Parse(jsonString);

                result.LatestVersion = releaseInfo?["tag_name"]?.ToString().TrimStart('v');
                result.ReleaseNotes = releaseInfo?["body"]?.ToString();
                result.DownloadUrl = releaseInfo?["assets"]?[0]?["browser_download_url"]?.ToString();

                if (Version.TryParse(result.CurrentVersion, out var currentVer) && Version.TryParse(result.LatestVersion, out var latestVer))
                {
                    result.IsUpdateAvailable = latestVer > currentVer;
                }
            }
            catch (Exception ex)
            {
                result.ErrorMessage = $"Bir hata oluştu: {ex.Message}";
            }

            return result;
        }

        public async Task DownloadAndPrepareUpdateAsync(string downloadUrl)
        {
            var updateTempPath = Path.Combine(_env.ContentRootPath, "_update");
            var zipPath = Path.Combine(updateTempPath, "update.zip");
            var extractPath = Path.Combine(updateTempPath, "new_version");

            // Geçici klasörleri temizle ve yeniden oluştur
            if (Directory.Exists(updateTempPath)) Directory.Delete(updateTempPath, true);
            Directory.CreateDirectory(updateTempPath);
            Directory.CreateDirectory(extractPath);

            // Yeni sürümü indir
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync(downloadUrl);
            using (var fs = new FileStream(zipPath, FileMode.Create))
            {
                await response.Content.CopyToAsync(fs);
            }

            // İndirilen zip dosyasını aç
            ZipFile.ExtractToDirectory(zipPath, extractPath, true);
            string settingsFile = Path.Combine(extractPath, "appsettings.json");
            if (System.IO.File.Exists(settingsFile))
            { 
                System.IO.File.Delete(settingsFile);
            }

            // Güncelleme bayrağını oluştur
            var flagPath = Path.Combine(_env.ContentRootPath, "update.flag");
            await File.WriteAllTextAsync(flagPath, "Update pending");
        }


        private string GetCurrentVersion()
        {
            // Projenin derlendiği assembly'den versiyon bilgisini okur.
            return Assembly.GetEntryAssembly()?.GetName().Version?.ToString(3) ?? "0.0.0";
        }
    }
}
