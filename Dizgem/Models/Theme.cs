using System.ComponentModel.DataAnnotations;

namespace Dizgem.Models
{
    public class Theme
    {
        // Temanın benzersiz kimliği
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        // Temanın dosya sistemi adı (örnek: 'default')
        public string Name { get; set; }

        // Temanın ekranda görünen adı (örnek: 'Varsayılan Tema')
        public string DisplayName { get; set; }

        // Temanın aktif olup olmadığını belirler
        public bool IsActive { get; set; }
    }
}
