namespace Dizgem.Models
{
    /// <summary>
    /// Yazı listeleme sayfasının (Index.cshtml) ana modelidir.
    /// Görüntülenecek yazı özetlerini ve sayfalama için gerekli bilgileri barındırır.
    /// </summary>
    public class PostIndexViewModel
    {
        public List<PostSummaryViewModel> Posts { get; set; } = new List<PostSummaryViewModel>();
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int? Year { get; set; }
        public int? Month { get; set; }
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
    }
}
