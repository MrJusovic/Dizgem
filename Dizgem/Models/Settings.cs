using System.ComponentModel.DataAnnotations;

namespace Dizgem.Models
{
    public class Settings
    {
        // Ayarın benzersiz kimliği
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        // Ayarın adı (örnek: 'SiteTitle')
        public string Key { get; set; }

        // Ayarın değeri (örnek: 'Dizgem - İçerik Yönetim Sistemi')
        public string Value { get; set; }
    }
}
