using Dizgem.Models;
using Microsoft.AspNetCore.Identity;

namespace Dizgem.Services
{
    public interface IPostService
    {
        /// <summary>
        /// Yayınlanmış yazıları sayfalanmış ve optimize edilmiş olarak alır.
        /// </summary>
        /// <param name="page">Geçerli sayfa numarası</param>
        /// <param name="pageSize">Sayfa başına gösterilecek yazı sayısı</param>
        /// <returns>Sayfalama bilgileri ve yazı özetlerini içeren bir ViewModel</returns>
        Task<PostIndexViewModel> GetPublishedPostsAsync(int page = 1, int pageSize = 10, int? year = null, int? month = null);

        /// <summary>
        /// Bir slug'a göre yayınlanmış bir yazının tüm detaylarını alır.
        /// </summary>
        /// <param name="slug">Yazının benzersiz slug değeri</param>
        /// <returns>İlgili Post nesnesi</returns>
        Task<Post> GetPostBySlugAsync(string slug);

        /// <summary>
        /// Tüm yayınlanmış yazıları yıl ve aylara göre gruplayarak bir arşiv listesi oluşturur.
        /// </summary>
        Task<IEnumerable<PostArchiveItemViewModel>> GetPostArchiveAsync();


        Task<PostEditViewModel<Post>> GetPostForEditAsync(Guid? postId);
        Task CreatePostAsync(PostEditViewModel<Post> model, Guid authorId);
        Task UpdatePostAsync(PostEditViewModel<Post> model);
        Task<bool> DeletePostAsync(Guid postId);
    }
}
