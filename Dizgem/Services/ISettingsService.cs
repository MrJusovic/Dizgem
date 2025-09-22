using Dizgem.Models;

namespace Dizgem.Services
{
    /// <summary>
    /// Uygulama genelindeki ayarları yönetir. Ayarları veritabanından yükler,
    /// önbelleğe alır ve günceller.
    /// </summary>
    public interface ISettingsService
    {
        /// <summary>
        /// Önbellekten mevcut site ayarlarını (SettingsViewModel olarak) getirir.
        /// </summary>
        SettingsViewModel Current { get; }

        /// <summary>
        /// Ayarları veritabanından yeniden yükleyerek önbelleği günceller.
        /// </summary>
        Task ReloadSettingsAsync();

        /// <summary>
        /// Verilen SettingsViewModel'ı veritabanındaki Setting kayıtlarına kaydeder ve önbelleği günceller.
        /// </summary>
        Task SaveSettingsAsync(SettingsViewModel settings);
    }
}
