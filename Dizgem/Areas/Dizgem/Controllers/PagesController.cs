using Dizgem.Data;
using Dizgem.Models;
using Dizgem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Dizgem.Areas.Dizgem.Controllers
{
    [Area("Dizgem")]
    [Authorize]
    public class PagesController : Controller
    {
        private readonly IPageService _pageService;
        private readonly UserManager<User> _userManager;

        public PagesController(IPageService pageService, UserManager<User> userManager)
        {
            _pageService = pageService;
            _userManager = userManager;
        }

        // GET: Dizgem/Posts (READ - Listeleme)
        public async Task<IActionResult> Index()
        {
            return View();
        }

        // GET: Dizgem/Posts/Create (CREATE - Formu Gösterme)
        public async Task<IActionResult> Upsert(Guid? id)
        {
            var model = await _pageService.GetPageForEditAsync(id);
            if (id.HasValue && model.Post == null)
            {
                return NotFound();
            }
            if (model.Post == null && (id == null || id == Guid.Empty))
            {
                model.Post = new Page()
                {
                    Id = Guid.Empty,
                    AuthorId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)),
                    PublishedDate = DateTime.Now
                };
            }
            if (model.Post.AuthorId == null || model.Post.AuthorId == Guid.Empty)
            {
                model.Post.AuthorId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upsert(PostEditViewModel<Page> model)
        {
            // Navigasyon property'leri ModelState'i geçersiz kılmasın diye temizliyoruz.
            ModelState.Remove("Post.Author");
            ModelState.Remove("Post.PageCategories");
            ModelState.Remove("Post.PageTags");
            ModelState.Remove("Post.Content");
            ModelState.Remove("Post.Excerpt");
            ModelState.Remove("TagsString");

            if (ModelState.IsValid)
            {
                if (model.Post.Id == Guid.Empty)
                {
                    // Create işlemi
                    var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                    await _pageService.CreatePageAsync(model, userId);
                }
                else
                {
                    // Update işlemi
                    await _pageService.UpdatePageAsync(model);
                }

                return RedirectToAction(nameof(Index));
            }

            // Model geçerli değilse, kategori listesi gibi gerekli alanları tekrar doldur.
            var repopulatedModel = await _pageService.GetPageForEditAsync(model.Post.Id);
            model.AllCategories = repopulatedModel.AllCategories;
            return View(model);
        }


        // POST: Dizgem/Posts/Delete/5 (DELETE - Silme İşlemini Gerçekleştirme)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var success = await _pageService.DeletePageAsync(id);
            if (!success)
            {
                return Json(new { success = false, message = "Yazı bulunamadı veya silinirken bir hata oluştu." });
            }

            return Json(new { success = true, message = "Yazı başarıyla silindi." });
        }

        [HttpPost]
        public async Task<IActionResult> LoadPagesData()
        {
            var draw = Request.Form["draw"].FirstOrDefault();
            var start = Request.Form["start"].FirstOrDefault();
            var length = Request.Form["length"].FirstOrDefault();
            var sortColumn = Request.Form["columns[" + Request.Form["order[0][column]"].FirstOrDefault() + "][name]"].FirstOrDefault();
            var sortColumnDirection = Request.Form["order[0][dir]"].FirstOrDefault();
            var searchValue = Request.Form["search[value]"].FirstOrDefault();

            int pageSize = length != null ? Convert.ToInt32(length) : 0;
            int skip = start != null ? Convert.ToInt32(start) : 0;

            // Bu kısım da servise taşınabilir, şimdilik burada bırakıyoruz.
            var context = HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();

            int recordsTotal = await context.Pages.CountAsync();
            var pagesQuery = context.Pages.Include(p => p.Author).AsQueryable();

            if (!string.IsNullOrEmpty(searchValue))
            {
                pagesQuery = pagesQuery.Where(p => p.Title.Contains(searchValue) || p.Author.DisplayName.Contains(searchValue));
            }

            // Sıralama (Dinamik sıralama için kütüphane kullanmak daha iyidir)
            pagesQuery = pagesQuery.OrderByDescending(p => p.PublishedDate);

            int recordsFiltered = await pagesQuery.CountAsync();
            var data = await pagesQuery.Skip(skip).Take(pageSize).ToListAsync();

            var jsonData = new
            {
                draw = draw,
                recordsFiltered = recordsFiltered,
                recordsTotal = recordsTotal,
                data = data.Select(p => new
                {
                    id = p.Id,
                    title = p.Title,
                    author = p.Author.DisplayName,
                    publishedDate = p.PublishedDate.ToString("dd.MM.yyyy HH:mm"),
                    status = p.IsPublished ? "Yayınlandı" : "Taslak",
                    category = p.PageCategories != null && p.PageCategories.Any(x => x.IsPrimary) ? p.PageCategories.Where(x => x.IsPrimary).Select(x => x.Category.Name).ToString() : "Kategori Yok"
                })
            };

            return Ok(jsonData);
        }
    }
}
