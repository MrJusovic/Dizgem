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
    }
}
