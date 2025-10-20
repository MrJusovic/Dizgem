using Dizgem.Models;

namespace Dizgem.Services
{
    public interface ICommentService
    {
        /// <summary>
        /// Gelen bir yorumu, IP ve UserAgent bilgileriyle zenginleştirerek veritabanına kaydeder.
        /// </summary>
        Task<(bool Success, string Message)> PostCommentAsync(Comment comment);

        /// <summary>
        /// Belirli bir yazıya, sayfaya veya genel alana ait onaylanmış yorumları hiyerarşik bir yapıda getirir.
        /// </summary>
        Task<List<CommentViewModel>> GetCommentsAsync(Guid? postId, Guid? pageId);
    }
}
