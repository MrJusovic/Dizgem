using Dizgem.Data;
using Dizgem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Dizgem.Areas.Dizgem.Controllers
{
    [Area("Dizgem")]
    [Authorize]
    public class CategoriesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CategoriesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Dizgem/Categories
        public IActionResult Index()
        {
            return View();
        }

        // POST: Dizgem/Categories/LoadCategoriesData
        [HttpPost]
        public async Task<IActionResult> LoadCategoriesData()
        {
            try
            {
                var draw = Request.Form["draw"].FirstOrDefault();
                var start = Request.Form["start"].FirstOrDefault();
                var length = Request.Form["length"].FirstOrDefault();
                var searchValue = Request.Form["search[value]"].FirstOrDefault();

                int pageSize = length != null ? Convert.ToInt32(length) : 0;
                int skip = start != null ? Convert.ToInt32(start) : 0;

                var categoriesQuery = _context.Categories.AsQueryable();

                if (!string.IsNullOrEmpty(searchValue))
                {
                    categoriesQuery = categoriesQuery.Where(c => c.Name.Contains(searchValue) || c.Slug.Contains(searchValue));
                }

                int recordsTotal = await _context.Categories.CountAsync();
                int recordsFiltered = await categoriesQuery.CountAsync();

                var data = await categoriesQuery.Skip(skip).Take(pageSize).ToListAsync();

                var jsonData = new
                {
                    draw = draw,
                    recordsFiltered = recordsFiltered,
                    recordsTotal = recordsTotal,
                    data = data.Select(c => new
                    {
                        id = c.Id,
                        name = c.Name,
                        slug = c.Slug
                    })
                };
                return Ok(jsonData);
            }
            catch (Exception)
            {
                return new EmptyResult();
            }
        }

        // GET: Dizgem/Categories/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Dizgem/Categories/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Slug")] Category category)
        {
            if (!string.IsNullOrWhiteSpace(category.Slug))
            {
                var slugExists = await _context.Categories.AnyAsync(c => c.Slug == category.Slug);
                if (slugExists)
                {
                    ModelState.AddModelError("Slug", "Bu URL metni zaten kullanılıyor.");
                }
            }
            ModelState.Remove(nameof(Category.PostCategories));
            if (ModelState.IsValid)
            {
                category.Id = Guid.NewGuid();
                _context.Add(category);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(category);
        }

        // GET: Dizgem/Categories/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null) return NotFound();
            var category = await _context.Categories.FindAsync(id);
            if (category == null) return NotFound();
            return View(category);
        }

        // POST: Dizgem/Categories/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Id,Name,Slug")] Category category)
        {
            if (id != category.Id) return NotFound();

            if (!string.IsNullOrWhiteSpace(category.Slug))
            {
                var slugExists = await _context.Categories.AnyAsync(c => c.Slug == category.Slug && c.Id != category.Id);
                if (slugExists)
                {
                    ModelState.AddModelError("Slug", "Bu URL metni başka bir kategori tarafından kullanılıyor.");
                }
            }
            ModelState.Remove(nameof(Category.PostCategories));
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(category);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Categories.Any(e => e.Id == category.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(category);
        }

        // POST: Dizgem/Categories/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return Json(new { success = false, message = "Kategori bulunamadı." });
            }

            try
            {
                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Kategori başarıyla silindi." });
            }
            catch (DbUpdateException)
            {
                return Json(new { success = false, message = "Bu kategori yazılarda kullanıldığı için silinemez." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Bir hata oluştu: " + ex.Message });
            }
        }
    }
}
