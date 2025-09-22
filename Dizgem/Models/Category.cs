using System.ComponentModel.DataAnnotations;

namespace Dizgem.Models
{
    public class Category
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        [StringLength(150)]
        public string Slug { get; set; }

        public ICollection<PostCategory> PostCategories { get; set; } = new List<PostCategory>();
        public ICollection<PageCategory> PageCategories { get; set; } = new List<PageCategory>();
    }
}
