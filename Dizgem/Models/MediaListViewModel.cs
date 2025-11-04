namespace Dizgem.Models
{
    /// <summary>
    /// Medya kütüphanesi ana sayfasının (Index) ihtiyaç duyduğu tüm verileri taşır.
    /// Sayfalama (pagination) ve filtreleme bilgilerini içerir.
    /// </summary>
    public class MediaListViewModel
    {
        public List<MediaSummaryViewModel> MediaItems { get; set; } = new List<MediaSummaryViewModel>();

        // Sayfalama Bilgileri
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;

        // Filtreleme Bilgileri (Arayüzde tekrar göstermek için)
        public string SearchQuery { get; set; }
        public string FileType { get; set; }
    }
}
