using Dizgem.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Dizgem.Areas.Dizgem.Controllers
{
    [Area("Dizgem")]
    [Authorize(Roles = "Admin")]
    public class CommentsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CommentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> LoadCommentsData()
        {
            // jQuery DataTables için sunucu taraflı veri yükleme
            // ... (PostsController'daki LoadPostsData'ya benzer şekilde implemente edilebilir)
            var comments = await _context.Comments
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new { c.Id, c.AuthorName, c.Content, c.CreatedAt, c.IsApproved })
                .ToListAsync();

            return Json(new { data = comments });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(Guid id)
        {
            var comment = await _context.Comments.FindAsync(id);
            if (comment != null)
            {
                comment.IsApproved = true;
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var comment = await _context.Comments.FindAsync(id);
            if (comment != null)
            {
                // Alt yorumları da silmek için bir mantık eklenebilir
                _context.Comments.Remove(comment);
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }
    }
}
