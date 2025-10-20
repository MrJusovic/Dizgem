using Dizgem.Data;
using Dizgem.Models;
using Dizgem.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Dizgem.Controllers
{
    public class CommentsController : Controller
    {
        private readonly ICommentService _commentService;
        private readonly ApplicationDbContext _context;

        public CommentsController(ICommentService commentService, ApplicationDbContext context)
        {
            _commentService = commentService;
            _context = context;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Post(Comment comment)
        {
            ModelState.Remove("Page");
            ModelState.Remove("Post");
            ModelState.Remove("Parent");
            ModelState.Remove("IPAddress");
            ModelState.Remove("UserAgent");
            ModelState.Remove("PageComments");
            ModelState.Remove("PostComments");

            if (ModelState.IsValid)
            {
                var (success, message) = await _commentService.PostCommentAsync(comment);

                // Mesajı bir sonraki sayfada göstermek için TempData'ya ata
                if (success)
                    TempData["SuccessMessage"] = message;
                else
                    TempData["ErrorMessage"] = message;
            }
            else
            {
                TempData["ErrorMessage"] = "Lütfen tüm zorunlu alanları doldurun.";
            }

            // Kullanıcının yorum yaptığı sayfayı bul ve oraya geri yönlendir
            if (comment.PostId.HasValue)
            {
                var post = await _context.Posts.AsNoTracking().FirstOrDefaultAsync(p => p.Id == comment.PostId.Value);
                if (post != null)
                {
                    // /post/{slug} adresine yönlendir
                    return RedirectToAction("Detail", "Post", new { slug = post.Slug });
                }
            }
            else if (comment.PageId.HasValue)
            {
                var page = await _context.Pages.AsNoTracking().FirstOrDefaultAsync(p => p.Id == comment.PageId.Value);
                if (page != null)
                {
                    // /{slug} adresine yönlendir
                    return RedirectToAction("Detail", "Page", new { slug = page.Slug });
                }
            }

            // Eğer bir yazı veya sayfaya bağlı değilse (veya bulunamazsa), ana sayfaya yönlendir
            return RedirectToAction("Index", "Home");
        }
    }
}
