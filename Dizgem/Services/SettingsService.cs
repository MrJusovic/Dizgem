using Dizgem.Data;
using Dizgem.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Dizgem.Services
{
    public class SettingsService : ISettingsService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;
        private const string SettingsCacheKey = "SiteSettings";
        private readonly IConnectionStringProvider _connectionStringProvider;

        public SettingsService(ApplicationDbContext context, IMemoryCache cache, IConnectionStringProvider connectionStringProvider)
        {
            _context = context;
            _cache = cache;
            _connectionStringProvider = connectionStringProvider;
            // Uygulama ilk başladığında ayarları veritabanından yükle
            // Not: Bu, senkron bir beklemedir ve sadece başlangıçta kullanılır.
            LoadSettingsFromDb().GetAwaiter().GetResult();

        }

        public SettingsViewModel Current => _cache.Get<SettingsViewModel>(SettingsCacheKey) ?? new SettingsViewModel();

        public async Task ReloadSettingsAsync()
        {
            await LoadSettingsFromDb();
        }

        public async Task SaveSettingsAsync(SettingsViewModel settings)
        {
            // ViewModel'daki her bir property (SiteTitle, SmtpHost vb.) için
            // veritabanında bir Setting kaydı arar veya oluşturur.
            foreach (var prop in typeof(SettingsViewModel).GetProperties())
            {
                var key = prop.Name;
                var value = prop.GetValue(settings)?.ToString();

                var existingSetting = await _context.Settings.FirstOrDefaultAsync(s => s.Key == key);

                if (existingSetting != null)
                {
                    // Şifre alanı formda boş bırakılmışsa, veritabanındaki mevcut değeri GÜNCELLEME.
                    if (prop.Name == nameof(SettingsViewModel.SmtpPassword) && string.IsNullOrEmpty(value))
                    {
                        continue;
                    }
                    existingSetting.Value = value;
                }
                else
                {
                    if (value != null)
                    {
                        // Yeni bir ayar ise ekle.
                        _context.Settings.Add(new Settings { Key = key, Value = value });
                    }
                }
            }

            await _context.SaveChangesAsync();
            await ReloadSettingsAsync(); // Kaydettikten sonra önbelleği yenile
        }

        private async Task LoadSettingsFromDb()
        {
            var settingsViewModel = new SettingsViewModel();
            if (!string.IsNullOrWhiteSpace(_connectionStringProvider.Current))
            {
                var allSettingsFromDb = await _context.Settings.ToListAsync();

                // Veritabanındaki her bir Setting kaydını (Key-Value) ViewModel'daki ilgili property'ye ata
                foreach (var prop in typeof(SettingsViewModel).GetProperties())
                {
                    var settingFromDb = allSettingsFromDb.FirstOrDefault(s => s.Key == prop.Name);
                    if (settingFromDb != null && !string.IsNullOrEmpty(settingFromDb.Value))
                    {
                        try
                        {
                            // Değeri doğru tipe dönüştürerek ata (örneğin string "587" -> int 587)
                            var convertedValue = Convert.ChangeType(settingFromDb.Value, prop.PropertyType);
                            prop.SetValue(settingsViewModel, convertedValue);
                        }
                        catch
                        {
                            // Tip dönüşümü başarısız olursa bu ayarı atla.
                        }
                    }
                }

                // Hazırlanan ViewModel'ı belirli bir süre için önbelleğe al.
                _cache.Set(SettingsCacheKey, settingsViewModel, TimeSpan.FromDays(30));
            }
            
        }
    }
}
