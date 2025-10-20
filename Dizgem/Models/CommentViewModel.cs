namespace Dizgem.Models
{
    /// <summary>
    /// Yorumları hiyerarşik olarak View'a taşımak için kullanılan veri transfer nesnesi.
    /// </summary>
    public class CommentViewModel
    {
        public Guid Id { get; set; }
        public string AuthorName { get; set; }
        public string Content { get; set; }
        public DateTime CreatedAt { get; set; }
        public string TimeAgo { get; set; } // "5 dakika önce" gibi bir format için
        public List<CommentViewModel> Replies { get; set; } = new List<CommentViewModel>();

        public CommentViewModel(Comment comment)
        {
            Id = comment.Id;
            AuthorName = comment.AuthorName;
            Content = comment.Content;
            CreatedAt = comment.CreatedAt;
            TimeAgo = ToTimeAgo(comment.CreatedAt);
        }

        // Zamanı "x zaman önce" formatına çeviren basit bir yardımcı metot
        private string ToTimeAgo(DateTime dt)
        {
            TimeSpan span = DateTime.Now - dt;
            if (span.Days > 365) return $"{span.Days / 365} yıl önce";
            if (span.Days > 30) return $"{span.Days / 30} ay önce";
            if (span.Days > 0) return $"{span.Days} gün önce";
            if (span.Hours > 0) return $"{span.Hours} saat önce";
            if (span.Minutes > 0) return $"{span.Minutes} dakika önce";
            return "az önce";
        }
    }
}
