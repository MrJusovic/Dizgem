namespace Dizgem.Models
{
    /// <summary>
    /// Yazı listeleme sayfasında her bir yazı için sadece gerekli olan temel bilgileri içerir.
    /// Büyük metin alanlarını (ContentJson, ContentHtml) içermez.
    /// </summary>
    public class PostSummaryViewModel
    {
        public string Title { get; set; }
        public string Slug { get; set; }
        public string Excerpt { get; set; } = "";
        public string CoverPhotoUrl { get; set; } = "";
        public DateTime PublishedDate { get; set; }
        public string AuthorDisplayName { get; set; }
        public string PrimaryCategoryName { get; set; }
    }
}
