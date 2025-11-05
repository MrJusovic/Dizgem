using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Dizgem.Models
{
    /// <summary>
    /// Medya kütüphanesine yüklenen her bir dosyayı temsil eden veritabanı modeli.
    /// </summary>
    public class Media
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Dosyanın yüklendiği orijinal adı. Örn: "gun-batimi.jpg"
        /// </summary>
        [Required]
        [StringLength(255)]
        public string FileName { get; set; }

        /// <summary>
        /// Dosyanın MIME türü. Örn: "image/jpeg", "application/pdf"
        /// </summary>
        [Required]
        [StringLength(100)]
        public string FileType { get; set; }

        /// <summary>
        /// Dosyanın bayt cinsinden boyutu.
        /// </summary>
        public long FileSize { get; set; }

        /// <summary>
        /// Dosyanın sisteme yüklendiği tarih.
        /// </summary>
        public DateTime UploadedAt { get; set; } = DateTime.Now;

        // --- Meta Verileri ---

        /// <summary>
        /// Medya için kullanıcı tanımlı başlık.
        /// </summary>
        [StringLength(255)]
        public string Title { get; set; }

        /// <summary>
        /// Medya için kullanıcı tanımlı uzun açıklama.
        /// </summary>
        [StringLength(1000)]
        public string Description { get; set; }

        /// <summary>
        /// Görseller için "alt text" veya dosyalar için kısa açıklama.
        /// </summary>
        [StringLength(500)]
        public string AltText { get; set; }

        // --- Dosya Yolları ---

        /// <summary>
        /// Orijinal (full size) dosyanın sunucudaki göreceli yolu.
        /// Örn: "/uploads/2025/11/gun-batimi.jpg"
        /// </summary>
        [Required]
        [StringLength(500)]
        public string UrlFull { get; set; }

        /// <summary>
        /// Görselse, oluşturulan farklı boyutların yollarını JSON olarak saklar.
        /// Örn: {"thumbnail": "/uploads/...", "medium": "/uploads/..."}
        /// </summary>
        [AllowNull]
        public string ImageSizesJson { get; set; }

        /// <summary>
        /// ImageSizesJson alanını C# Dictionary olarak kullanılabilir hale getirir.
        /// </summary>
        [NotMapped]
        public Dictionary<string, string> ImageSizes
        {
            get => string.IsNullOrWhiteSpace(ImageSizesJson)
                ? new Dictionary<string, string>()
                : JsonSerializer.Deserialize<Dictionary<string, string>>(ImageSizesJson);
            set => ImageSizesJson = JsonSerializer.Serialize(value);
        }
        
        public string GetSize(string size)
        {
            if (ImageSizes.TryGetValue(size, out string imageUrl))
            {
                // 3. Bulunduysa, o boyutu döndür
                return imageUrl;
            }

            // 4. Bulunamadıysa (TryGetValue false döndürdüyse), varsayılan UrlFull'u döndür
            return UrlFull;
        }

        // --- İlişkiler ---

        /// <summary>
        /// Dosyayı yükleyen kullanıcının ID'si (opsiyonel).
        /// </summary>
        public Guid? UploaderUserId { get; set; }

        [ForeignKey("UploaderUserId")]
        public User Uploader { get; set; }
    }
}
