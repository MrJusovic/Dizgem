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
            var draw = Request.Form["draw"].FirstOrDefault();
            var start = Request.Form["start"].FirstOrDefault();
            var length = Request.Form["length"].FirstOrDefault();
            var searchValue = Request.Form["search[value]"].FirstOrDefault();

            int pageSize = length != null ? Convert.ToInt32(length) : 0;
            int skip = start != null ? Convert.ToInt32(start) : 0;

            var query = _context.Comments
                .Include(c => c.Post) // Yorumun ait olduğu yazıyı da al
                .Include(c => c.Page) // Veya sayfayı
                .AsNoTracking();

            if (!string.IsNullOrEmpty(searchValue))
            {
                query = query.Where(c => c.AuthorName.Contains(searchValue) || c.AuthorEmail.Contains(searchValue) || c.Content.Contains(searchValue));
            }

            int recordsTotal = await query.CountAsync();

            var data = await query.OrderByDescending(c => c.CreatedAt)
                                  .Skip(skip)
                                  .Take(pageSize)
                                  .ToListAsync();

            var jsonData = new
            {
                draw = draw,
                recordsFiltered = recordsTotal,
                recordsTotal = recordsTotal,
                data = data.Select(c => new
                {
                    c.Id,
                    c.AuthorName,
                    Content = c.Content.Length > 100 ? c.Content.Substring(0, 100) + "..." : c.Content,
                    CreatedAt = c.CreatedAt.ToString("dd.MM.yyyy HH:mm"),
                    c.IsApproved,
                    // Yorumun nereye yapıldığını belirt
                    Source = c.PostId.HasValue ? $"Yazı: {c.Post.Title}" : (c.PageId.HasValue ? $"Sayfa: {c.Page.Title}" : "Genel")
                })
            };

            return Ok(jsonData);
        }

        [HttpGet]
        public async Task<IActionResult> Detail(Guid id)
        {
            var comment = await _context.Comments
                .Include(c => c.Post)
                .Include(c => c.Page)
                .Include(c => c.Parent)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id);

            if (comment == null)
            {
                return NotFound();
            }

            return View(comment);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleApproval(Guid id)
        {
            var comment = await _context.Comments.FindAsync(id);
            if (comment == null)
            {
                return Json(new { success = false, message = "Yorum bulunamadı." });
            }

            comment.IsApproved = !comment.IsApproved;
            await _context.SaveChangesAsync();

            return Json(new { success = true, isApproved = comment.IsApproved });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var commentToDelete = await _context.Comments.FindAsync(id);
            if (commentToDelete == null)
            {
                return Json(new { success = false, message = "Silinecek yorum bulunamadı." });
            }

            // Bu yoruma bağlı tüm alt yorumları (cevapları) da bul ve sil
            var repliesToDelete = await _context.Comments
                .Where(c => c.ParentId == id)
                .ToListAsync();

            if (repliesToDelete.Any())
            {
                _context.Comments.RemoveRange(repliesToDelete);
            }

            _context.Comments.Remove(commentToDelete);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Yorum başarıyla silindi." });
        }
    }
}
