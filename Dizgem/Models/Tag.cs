using System.ComponentModel.DataAnnotations;

namespace Dizgem.Models
{
    public class Tag
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        [StringLength(150)]
        public string Slug { get; set; }

        public ICollection<PostTag> PostTags { get; set; }
        public ICollection<PageTag> PageTags { get; set; }
    }
}
