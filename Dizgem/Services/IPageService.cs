using Dizgem.Models;

namespace Dizgem.Services
{
    public interface IPageService
    {
        /// <summary>
        /// Yayınlanmış yazıları sayfalanmış ve optimize edilmiş olarak alır.
        /// </summary>
        /// <param name="page">Geçerli sayfa numarası</param>
        /// <param name="pageSize">Sayfa başına gösterilecek yazı sayısı</param>
        /// <returns>Sayfalama bilgileri ve yazı özetlerini içeren bir ViewModel</returns>
        Task<PostIndexViewModel> GetPublishedPagesAsync(int page = 1, int pageSize = 10, int? year = null, int? month = null);

        /// <summary>
        /// Bir slug'a göre yayınlanmış bir yazının tüm detaylarını alır.
        /// </summary>
        /// <param name="slug">Yazının benzersiz slug değeri</param>
        /// <returns>İlgili Post nesnesi</returns>
        Task<Page> GetPageBySlugAsync(string slug);

        /// <summary>
        /// Tüm yayınlanmış yazıları yıl ve aylara göre gruplayarak bir arşiv listesi oluşturur.
        /// </summary>
        Task<IEnumerable<PostArchiveItemViewModel>> GetPageArchiveAsync();


        Task<PostEditViewModel<Page>> GetPageForEditAsync(Guid? postId);
        Task CreatePageAsync(PostEditViewModel<Page> model, Guid authorId);
        Task UpdatePageAsync(PostEditViewModel<Page> model);
        Task<bool> DeletePageAsync(Guid postId);
    }
}
