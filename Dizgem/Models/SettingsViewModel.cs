using System.ComponentModel.DataAnnotations;

namespace Dizgem.Models
{
    /// <summary>
    /// Yönetim panelindeki ayarlar sayfasının formunu ve veritabanından yüklenen
    /// ayarları güçlü bir şekilde (strongly-typed) temsil etmek için kullanılır.
    /// </summary>
    public class SettingsViewModel
    {
        // --- Genel Site Ayarları ---
        [Display(Name = "Site Başlığı")]
        public string SiteTitle { get; set; } = "Dizgem";

        [Display(Name = "Site Açıklaması")]
        public string SiteDescription { get; set; } = "Dizgem ile güçlendirilmiştir.";

        [Display(Name = "Favicon URL")]
        public string? FaviconUrl { get; set; }

        //[Display(Name = "Aktif Tema")]
        //public string ActiveTheme { get; set; } = "Default";

        [Display(Name = "Varsayılan Sosyal Medya Paylaşım Resmi URL")]
        public string? SiteImageUrl { get; set; }

        [Display(Name = "Twitter Kullanıcı Adı (@ olmadan)")]
        public string? TwitterHandle { get; set; }

        // --- SMTP E-posta Ayarları ---
        [Display(Name = "SMTP Sunucusu")]
        public string? SmtpHost { get; set; }

        [Display(Name = "SMTP Portu")]
        public int SmtpPort { get; set; } = 587;

        [Display(Name = "SMTP Kullanıcı Adı")]
        public string? SmtpUser { get; set; }

        [Display(Name = "SMTP Şifresi")]
        [DataType(DataType.Password)]
        public string? SmtpPassword { get; set; }

        [Display(Name = "SSL/TLS Kullan")]
        public bool SmtpUseSsl { get; set; } = true;

        [Display(Name = "Gönderen E-posta Adresi")]
        [EmailAddress]
        public string? SmtpFromEmail { get; set; }
    }
}
