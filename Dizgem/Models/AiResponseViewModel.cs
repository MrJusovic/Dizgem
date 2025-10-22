namespace Dizgem.Models
{
    public class AiResponseViewModel
    {
        /// <summary>
        /// İşlemin başarılı olup olmadığını belirtir.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// İşlem başarılıysa, AI tarafından üretilen ana metin.
        /// (Örn: Metin iyileştirme için kullanılır)
        /// </summary>
        public string GeneratedText { get; set; }

        /// <summary>
        /// AI tarafından önerilen başlıklar veya diğer metin önerileri listesi.
        /// (Örn: Başlık önerme için kullanılır)
        /// </summary>
        public List<string> Suggestions { get; set; }

        /// <summary>
        /// AI tarafından üretilen özet metni.
        /// (Örn: Özet/SEO oluşturma için kullanılır)
        /// </summary>
        public string GeneratedSummary { get; set; }

        /// <summary>
        /// AI tarafından üretilen SEO açıklaması metni.
        /// (Örn: Özet/SEO oluşturma için kullanılır)
        /// </summary>
        public string GeneratedDescription { get; set; }

        /// <summary>
        /// AI tarafından üretilen kapak fotoğrafının URL'si (genellikle base64 data URL).
        /// (Örn: Kapak fotoğrafı oluşturma için kullanılır)
        /// </summary>
        public string GeneratedImageUrl { get; set; }


        /// <summary>
        /// İşlem başarısızsa, hata mesajı.
        /// </summary>
        public string ErrorMessage { get; set; }
    }
}
