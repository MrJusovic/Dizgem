using System.ComponentModel.DataAnnotations;

namespace Dizgem.Models
{
    /// <summary>
    /// Bir menü içindeki tek bir linki temsil eder.
    /// </summary>
    public class MenuItem
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid MenuId { get; set; }
        public Menu Menu { get; set; }

        /// <summary>
        /// Alt menü oluşturmak için üst öğenin ID'si.
        /// </summary>
        public Guid? ParentId { get; set; }
        public MenuItem Parent { get; set; }

        [Required]
        [StringLength(100)]
        public string Title { get; set; }

        [StringLength(100)]
        public string Label { get; set; }

        [Required]
        public string Url { get; set; }

        public int Order { get; set; }

        [StringLength(100)]
        public string? CssClass { get; set; }

        [StringLength(100)]
        public string? ElementId { get; set; }

        public ICollection<MenuItem> Children { get; set; }
    }
}
