using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dizgem.Models
{
    public class Comment
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        // Yorumun hangi yazıya ait olduğu (yazı yorumu ise)
        public Guid? PostId { get; set; }
        [ForeignKey("PostId")]
        public Post Post { get; set; }

        // Yorumun hangi sayfaya ait olduğu (sayfa yorumu ise)
        public Guid? PageId { get; set; }
        [ForeignKey("PageId")]
        public Page Page { get; set; }

        // Bu yorumun hangi yoruma cevap olduğu (alt yorum ise)
        public Guid? ParentId { get; set; }
        [ForeignKey("ParentId")]
        public Comment Parent { get; set; }

        [Required(ErrorMessage = "İsim alanı zorunludur.")]
        [StringLength(100)]
        public string AuthorName { get; set; }

        [Required(ErrorMessage = "E-posta alanı zorunludur.")]
        [EmailAddress]
        [StringLength(100)]
        public string AuthorEmail { get; set; }

        [Required(ErrorMessage = "Yorum içeriği boş olamaz.")]
        [StringLength(2000)]
        public string Content { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public bool IsApproved { get; set; } = false;

        
        /// <summary>
        /// Yorumu gönderen kullanıcının IP adresi.
        /// </summary>
        [StringLength(45)] // IPv6 adreslerini de desteklemek için
        public string IpAddress { get; set; }

        /// <summary>
        /// Yorumu gönderen kullanıcının tarayıcı ve işletim sistemi bilgisi.
        /// </summary>
        [StringLength(255)]
        public string UserAgent { get; set; }

        // Bu yoruma verilen cevapları tutar
        public ICollection<Comment> Replies { get; set; } = new List<Comment>();

        public ICollection<PostComment> PostComments { get; set; }
        public ICollection<PageComment> PageComments { get; set; }
    }
}
