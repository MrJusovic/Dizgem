using System.ComponentModel.DataAnnotations;

namespace Dizgem.Models
{
    /// <summary>
    /// Bir menü yapısını temsil eder (örn: Ana Menü).
    /// </summary>
    public class Menu
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        /// <summary>
        /// Temada tanımlanan menü konumu (örn: 'primary-menu').
        /// </summary>
        [Required]
        [StringLength(100)]
        public string LocationId { get; set; }

        // Menüye ait öğeler.
        public ICollection<MenuItem> MenuItems { get; set; }
    }
}
