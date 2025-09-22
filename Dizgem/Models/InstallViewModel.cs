using System.ComponentModel.DataAnnotations;

namespace Dizgem.Models
{
    public class InstallViewModel
    {
        // Adım 1: Veritabanı Bağlantı Bilgileri
        [Required(ErrorMessage = "Veritabanı sunucu adresi zorunludur.")]
        public string DatabaseServer { get; set; }

        [Required(ErrorMessage = "Veritabanı adı zorunludur.")]
        public string DatabaseName { get; set; }

        [Required(ErrorMessage = "Veritabanı kullanıcı adı zorunludur.")]
        public string DatabaseUser { get; set; }

        [Required(ErrorMessage = "Veritabanı şifresi zorunludur.")]
        public string DatabasePassword { get; set; }

        // Adım 2: Site Ayarları
        [Required(ErrorMessage = "Site başlığı zorunludur.")]
        public string SiteTitle { get; set; }
        public string SiteDescription { get; set; }

        // Adım 3: Yönetici Kullanıcısı
        [Required(ErrorMessage = "Yönetici kullanıcı adı zorunludur.")]
        public string AdminUsername { get; set; }

        [Required(ErrorMessage = "Yönetici şifresi zorunludur.")]
        [MinLength(6, ErrorMessage = "Şifre en az 6 karakter olmalıdır.")]
        public string AdminPassword { get; set; }

        [Required(ErrorMessage = "Yönetici e-posta adresi zorunludur.")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi girin.")]
        public string AdminEmail { get; set; }
    }
}
