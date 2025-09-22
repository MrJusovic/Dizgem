using Microsoft.AspNetCore.Html;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dizgem.Models
{
    public class Post
    {
        // Yazının benzersiz kimliği (Primary Key)
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        // Yazının başlığı
        public string Title { get; set; }


        // === YENİ EKLENEN ALANLAR ===
        [Required(ErrorMessage = "Özet alanı zorunludur.")]
        public string Excerpt { get; set; }

        [Display(Name = "Kapak Fotoğrafı URL")]
        public string? CoverPhotoUrl { get; set; }

        // Editor.js'in ham JSON çıktısını burada saklayacağız.
        // Bu alan "source of truth" (verinin asıl kaynağı) olacak.
        public string ContentJson { get; set; }

        // Yazının içeriği
        public string Content { get; set; }

        [NotMapped]
        public HtmlString RenderedContent => new HtmlString(Content);

        // SEO dostu URL (örnek: 'ilk-yazimiz')
        public string Slug { get; set; }

        // Yazının yayınlanma tarihi
        public DateTime PublishedDate { get; set; } = DateTime.Now;

        // Yazının durumu (taslak, yayınlandı)
        public bool IsPublished { get; set; } = false;

        // Yazara ait kullanıcı ID'si
        public Guid AuthorId { get; set; }

        // Yazar bilgisine erişim için navigasyon özelliği
        public User Author { get; set; }


        // === SEO Alanları ===
        [StringLength(255)]
        public string? SeoTitle { get; set; }

        [StringLength(500)]
        public string? SeoDescription { get; set; }

        [StringLength(255)]
        public string? SeoKeywords { get; set; }

        // === İlişkisel Alanlar ===
        public ICollection<PostCategory> PostCategories { get; set; }
        public ICollection<PostTag> PostTags { get; set; }
    }
}
