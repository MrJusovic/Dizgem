using Dizgem.Data;
using Dizgem.Helpers;
using Dizgem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Dizgem.Areas.Dizgem.Controllers
{
    [Area("Dizgem")]
    [Authorize]
    public class TagsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TagsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Dizgem/Tags
        // Sadece DataTables'ın çalışacağı boş tabloyu içeren View'ı döndürür.
        public IActionResult Index()
        {
            return View();
        }

        // POST: Dizgem/Tags/LoadTagsData
        // jQuery DataTables'ın sunucu taraflı veri çekme isteğini karşılar.
        [HttpPost]
        public async Task<IActionResult> LoadTagsData()
        {
            try
            {
                var draw = Request.Form["draw"].FirstOrDefault();
                var start = Request.Form["start"].FirstOrDefault();
                var length = Request.Form["length"].FirstOrDefault();
                var sortColumn = Request.Form["columns[" + Request.Form["order[0][column]"].FirstOrDefault() + "][name]"].FirstOrDefault();
                var sortColumnDirection = Request.Form["order[0][dir]"].FirstOrDefault();
                var searchValue = Request.Form["search[value]"].FirstOrDefault();

                int pageSize = length != null ? Convert.ToInt32(length) : 0;
                int skip = start != null ? Convert.ToInt32(start) : 0;

                // Toplam kayıt sayısı (filtrelenmemiş)
                int recordsTotal = await _context.Tags.CountAsync();

                // Veri sorgusu
                var tagsQuery = _context.Tags.AsQueryable();

                // Arama (Filtreleme)
                if (!string.IsNullOrEmpty(searchValue))
                {
                    tagsQuery = tagsQuery.Where(t => t.Name.Contains(searchValue) || t.Slug.Contains(searchValue));
                }

                // Sıralama
                if (!(string.IsNullOrEmpty(sortColumn) && string.IsNullOrEmpty(sortColumnDirection)))
                {
                    switch (sortColumn)
                    {
                        case "name":
                            tagsQuery = sortColumnDirection == "asc" ? tagsQuery.OrderBy(p => p.Name) : tagsQuery.OrderByDescending(p => p.Name);
                            break;
                        case "slug":
                            tagsQuery = sortColumnDirection == "asc" ? tagsQuery.OrderBy(p => p.Slug) : tagsQuery.OrderByDescending(p => p.Slug);
                            break;
                        default:
                            tagsQuery = tagsQuery.OrderBy(p => p.Name);
                            break;
                    }
                }

                // Filtrelenmiş kayıt sayısı
                int recordsFiltered = await tagsQuery.CountAsync();

                // Sayfalama
                var data = await tagsQuery.Skip(skip).Take(pageSize).ToListAsync();

                // DataTables'ın beklediği formata uygun JSON verisi
                var jsonData = new
                {
                    draw = draw,
                    recordsFiltered = recordsFiltered,
                    recordsTotal = recordsTotal,
                    data = data.Select(t => new
                    {
                        id = t.Id,
                        name = t.Name,
                        slug = t.Slug
                    })
                };

                return Ok(jsonData);
            }
            catch (Exception ex)
            {
                LoggerHelper.LogException(ex);
                return new EmptyResult();
            }
        }


        // GET: Dizgem/Tags/Create
        public IActionResult Create() 
        {
            return View();
        }

        // POST: Dizgem/Tags/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Slug")] Tag tag)
        {
            // --- YENİ EKLENEN KONTROL ---
            if (!string.IsNullOrWhiteSpace(tag.Slug))
            {
                var slugExists = await _context.Tags.AnyAsync(t => t.Slug == tag.Slug);
                if (slugExists)
                {
                    ModelState.AddModelError("Slug", "Bu URL metni zaten kullanılıyor.");
                }
            }
            // -----------------------------
            ModelState.Remove(nameof(Tag.PostTags));
            if (ModelState.IsValid)
            {
                tag.Id = Guid.NewGuid();
                _context.Add(tag);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(tag);
        }

        // GET: Dizgem/Tags/Edit/{id}
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var tag = await _context.Tags.FindAsync(id);
            if (tag == null)
            {
                return NotFound();
            }
            return View(tag);
        }

        // POST: Dizgem/Tags/Edit/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Id,Name,Slug")] Tag tag)
        {
            if (id != tag.Id)
            {
                return NotFound();
            }

            // --- YENİ EKLENEN KONTROL ---
            if (!string.IsNullOrWhiteSpace(tag.Slug))
            {
                // Mevcut etiket HARİÇ, başka bir etiketin bu slug'a sahip olup olmadığını kontrol et
                var slugExists = await _context.Tags.AnyAsync(t => t.Slug == tag.Slug && t.Id != tag.Id);
                if (slugExists)
                {
                    ModelState.AddModelError("Slug", "Bu URL metni başka bir etiket tarafından kullanılıyor.");
                }
            }
            // -----------------------------
            ModelState.Remove(nameof(Tag.PostTags));
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(tag);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    LoggerHelper.LogException(ex);
                    if (!TagExists(tag.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(tag);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var tag = await _context.Tags.FindAsync(id);
            if (tag == null)
            {
                return Json(new { success = false, message = "Etiket bulunamadı." });
            }

            try
            {
                _context.Tags.Remove(tag);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Etiket başarıyla silindi." });
            }
            catch (DbUpdateException ex)
            {
                LoggerHelper.LogException(ex);
                // Bu hata genellikle etiket başka bir tabloda (PostTags) kullanılıyorsa oluşur.
                return Json(new { success = false, message = "Bu etiket yazılarda kullanıldığı için silinemez." });
            }
            catch (Exception ex)
            {
                // Diğer beklenmedik hatalar için loglama yapılabilir.
                return Json(new { success = false, message = "Bir hata oluştu: " + ex.Message });
            }
        }

        private bool TagExists(Guid id)
        {
            return _context.Tags.Any(e => e.Id == id);
        }
    }
}
