using Microsoft.AspNetCore.Html;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dizgem.Models
{
    public class Page : ISeoContent
    {
        // Sayfanın benzersiz kimliği (Primary Key)
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        // Yazının başlığı
        public string Title { get; set; }


        [Required(ErrorMessage = "Özet alanı zorunludur.")]
        public string Excerpt { get; set; }

        [Display(Name = "Kapak Fotoğrafı")]
        public Guid? CoverPhotoMediaId { get; set; }

        [ForeignKey("CoverPhotoMediaId")]
        public virtual Media CoverPhoto { get; set; }

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

        [Display(Name = "Yorum Politikası")]
        public CommentStatus CommentPolicy { get; set; } = CommentStatus.UseGlobal;

        public ICollection<PageComment> Comments { get; set; }

        [NotMapped]
        public bool AreCommentsEnabled { get; set; } = true;

        // === SEO Alanları ===
        [StringLength(255)]
        public string? SeoTitle { get; set; }

        [StringLength(500)]
        public string? SeoDescription { get; set; }

        [StringLength(255)]
        public string? SeoKeywords { get; set; }

        // === İlişkisel Alanlar ===
        public ICollection<PageCategory> PageCategories { get; set; }
        public ICollection<PageTag> PageTags { get; set; }
    }
}
