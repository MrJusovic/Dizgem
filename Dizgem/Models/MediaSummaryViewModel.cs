namespace Dizgem.Models
{
    /// <summary>
    /// Medya kütüphanesi listeleme ekranında (grid view)
    /// her bir öğeyi temsil etmek için kullanılan hafif model.
    /// </summary>
    public class MediaSummaryViewModel
    {
        public Guid Id { get; set; }
        public string FileName { get; set; }
        public string FileType { get; set; }
        public string Title { get; set; }

        /// <summary>
        /// Liste görünümünde gösterilecek olan önizleme görselinin URL'si.
        /// (Görsel değilse, dosya tipi ikonu yolu olabilir).
        /// </summary>
        public string ThumbnailUrl { get; set; }
    }
}
