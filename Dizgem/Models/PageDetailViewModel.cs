namespace Dizgem.Models
{
    /// <summary>
    /// Yazı detay sayfasının ihtiyaç duyduğu tüm verileri bir arada tutar.
    /// </summary>
    public class PageDetailViewModel
    {
        /// <summary>
        /// Görüntülenen yazı nesnesi.
        /// </summary>
        public Page Page { get; set; }

        /// <summary>
        /// Bu yazı için yorumların gösterilip gösterilmeyeceğini belirtir.
        /// Bu değer, servis katmanında hem genel ayarlar hem de yazının kendi
        /// CommentPolicy'si dikkate alınarak hesaplanır.
        /// </summary>
        public bool AreCommentsEnabled { get; set; }
    }
}
