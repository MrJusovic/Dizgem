using System.ComponentModel.DataAnnotations;

namespace Dizgem.Models
{
    /// <summary>
    /// Bir medya dosyasının detaylarını göstermek ve meta verilerini
    /// düzenlemek için kullanılan model.
    /// </summary>
    public class MediaDetailViewModel
    {
        public Guid Id { get; set; }
        public string FileName { get; set; }
        public string FileType { get; set; }
        public long FileSize { get; set; } // Bayt cinsinden
        public DateTime UploadedAt { get; set; }
        public string UploaderName { get; set; } // Yükleyenin adı

        [Display(Name = "Başlık")]
        [StringLength(255)]
        public string Title { get; set; }

        [Display(Name = "Açıklama")]
        [StringLength(1000)]
        public string Description { get; set; }

        [Display(Name = "Alternatif Metin (Alt Text)")]
        [StringLength(500)]
        public string AltText { get; set; }

        /// <summary>
        /// Orijinal dosyanın tam URL'si.
        /// </summary>
        public string UrlFull { get; set; }

        /// <summary>
        /// Görsel boyutlarının (thumbnail, medium vb.) URL'lerini tutan sözlük.
        /// </summary>
        public Dictionary<string, string> ImageSizes { get; set; } = new Dictionary<string, string>();
    }
}
