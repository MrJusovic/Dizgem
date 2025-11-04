using Dizgem.Models;

namespace Dizgem.Services
{
    /// <summary>
    /// Medya (görsel, dosya vb.) yükleme, listeleme, silme ve yönetme
    /// ile ilgili tüm işlemleri yürüten servis arayüzü.
    /// </summary>
    public interface IMediaService
    {
        /// <summary>
        /// Yeni bir dosyayı (görsel, pdf vb.) sisteme yükler.
        /// Eğer dosya bir görsel ise, otomatik olarak "thumbnail", "medium", "large"
        /// "light", "@2x" ve "full" boyutlarını oluşturur ve kaydeder.
        /// </summary>
        /// <param name="file">Yüklenecek dosya.</param>
        /// <param name="uploaderUserId">Dosyayı yükleyen yöneticinin (opsiyonel) ID'si.</param>
        /// <returns>Oluşturulan medya kaydının veritabanı modelini döndürür.</returns>
        Task<Media> UploadFileAsync(IFormFile file, Guid? uploaderUserId);

        /// <summary>
        /// Medya kütüphanesindeki dosyaları sayfalı (paginated) ve aranabilir bir yapıda listeler.
        /// </summary>
        /// <param name="page">Hangi sayfa</param>
        /// <param name="pageSize">Sayfa başına kaç dosya</param>
        /// <param name="searchQuery">Arama sorgusu (dosya adı, başlık vb.)</param>
        /// <param name="fileType">Dosya tipine göre filtrele (image, document, video vb.)</param>
        /// <returns>Dosyaların listesini ve toplam sayfa bilgisini içeren bir ViewModel döndürür.</returns>
        Task<MediaListViewModel> GetMediaListAsync(int page, int pageSize, string searchQuery, string fileType);

        /// <summary>
        /// Belirli bir medya dosyasının tüm detaylarını (meta verileri dahil) getirir.
        /// </summary>
        /// <param name="id">Medya dosyasının benzersiz ID'si.</param>
        /// <returns>Medya detaylarını içeren bir ViewModel döndürür.</returns>
        Task<MediaDetailViewModel> GetMediaByIdAsync(Guid id);

        /// <summary>
        /// Bir medya dosyasının meta verilerini (Başlık, Açıklama, Kısa Açıklama/Alt Metin) günceller.
        /// </summary>
        /// <param name="id">Medya dosyasının ID'si.</param>
        /// <param name="title">Yeni başlık.</param>
        /// <param name="description">Yeni uzun açıklama.</param>
        /// <param name_blank="altText">Yeni kısa açıklama / "alt" metni.</param>
        /// <returns>İşlemin başarılı olup olmadığını ve bir mesaj döndürür.</returns>
        Task<(bool Success, string Message)> UpdateMetadataAsync(Guid id, string title, string description, string altText);

        /// <summary>
        /// Bir medya dosyasını (eğer görselse tüm boyutları dahil) sunucudan ve
        /// kaydını veritabanından kalıcı olarak siler.
        /// </summary>
        /// <param name="id">Medya dosyasının ID'si.</param>
        /// <returns>İşlemin başarılı olup olmadığını ve bir mesaj döndürür.</returns>
        Task<(bool Success, string Message)> DeleteFileAsync(Guid id);

        /// <summary>
        /// Belirli bir görsel için tüm boyutları (thumbnail, medium, large vb.)
        /// sistem ayarlarındaki güncel boyutlara göre yeniden oluşturur.
        /// </summary>
        /// <param name="id">Medya (görsel) dosyasının ID'si.</param>
        /// <returns>İşlemin başarılı olup olmadığını ve bir mesaj döndürür.</returns>
        Task<(bool Success, string Message)> RegenerateThumbnailsAsync(Guid id);
    }
}
