using Dizgem.Models;

namespace Dizgem.Services
{
    public interface IThemeService
    {
        // Dosya sisteminden yüklü temaların listesini döndürür.
        Task<List<ThemeViewModel>> GetInstalledThemesAsync();

        // Veritabanında aktif temayı ayarlar.
        Task ActivateThemeAsync(string themeName);

        // Veritabanından aktif temanın adını alır.
        Task<string> GetActiveThemeNameAsync();

        /// <summary>
        /// Yüklenen bir tema zip dosyasını sisteme kurar.
        /// </summary>
        /// <param name="themeZipFile">Yüklenen .zip dosyası.</param>
        /// <returns>İşlemin başarısını ve bir mesaj içeren bir tuple.</returns>
        Task<(bool Success, string Message)> InstallThemeAsync(IFormFile themeZipFile);
        Task<(bool Success, string Message)> DeleteThemeAsync(string themeName);
    }
}
