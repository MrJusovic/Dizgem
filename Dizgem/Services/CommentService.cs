using Dizgem.Data;
using Dizgem.Models;
using Microsoft.EntityFrameworkCore;

namespace Dizgem.Services
{
    public class CommentService : ICommentService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CommentService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<(bool Success, string Message)> PostCommentAsync(Comment comment)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext != null)
            {
                // Yasal bilgiler için IP ve User Agent'ı al
                comment.IpAddress = httpContext.Connection.RemoteIpAddress?.ToString();
                comment.UserAgent = httpContext.Request.Headers["User-Agent"].ToString();
            }

            // Varsayılan olarak yorumlar onaya düşsün.
            comment.IsApproved = false;

            await _context.Comments.AddAsync(comment);

            if (comment.PostId != null && comment.PostId != Guid.Empty)
            {
                _context.PostComments.Add(new PostComment() { PostId = (Guid)comment.PostId, CommentId = comment.Id });
            }
            if (comment.PageId != null && comment.PageId != Guid.Empty)
            {
                _context.PageComments.Add(new PageComment() { PageId = (Guid)comment.PageId, CommentId = comment.Id });
            }

            await _context.SaveChangesAsync();

            return (true, "Yorumunuz alındı ve onaylandıktan sonra yayınlanacaktır.");
        }

        public async Task<List<CommentViewModel>> GetCommentsAsync(Guid? postId, Guid? pageId)
        {
            var query = _context.Comments.AsNoTracking().Where(c => c.IsApproved);

            if (postId.HasValue)
                query = query.Where(c => c.PostId == postId.Value);
            else if (pageId.HasValue)
                query = query.Where(c => c.PageId == pageId.Value);
            else // Genel yorumlar (ne post ne de page ile ilişkili olmayanlar)
                query = query.Where(c => c.PostId == null && c.PageId == null);

            var flatComments = await query.OrderByDescending(c => c.CreatedAt).ToListAsync();

            // Düz listeyi hiyerarşik yapıya dönüştür
            var commentViewModels = flatComments.Where(c => c.ParentId == null)
                                                 .Select(c => new CommentViewModel(c))
                                                 .ToList();

            foreach (var vm in commentViewModels)
            {
                vm.Replies = GetReplies(vm, flatComments);
            }

            return commentViewModels;
        }

        private List<CommentViewModel> GetReplies(CommentViewModel parent, List<Comment> allComments)
        {
            return allComments.Where(c => c.ParentId == parent.Id)
                              .Select(c => {
                                  var vm = new CommentViewModel(c);
                                  vm.Replies = GetReplies(vm, allComments); // Özyinelemeli (Recursive) çağrı
                                  return vm;
                              }).ToList();
        }
    }
}
