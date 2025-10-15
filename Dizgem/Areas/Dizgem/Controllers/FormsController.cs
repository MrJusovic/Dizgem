using Dizgem.Data;
using Dizgem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Dizgem.Areas.Dizgem.Controllers
{
    [Area("Dizgem")]
    [Authorize]
    public class FormsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public FormsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new FormHandler());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(FormHandler formHandler)
        {
            ModelState.Remove("UniqueIdentifier");
            if (ModelState.IsValid)
            {
                // Benzersiz tanımlayıcıyı otomatik oluştur
                formHandler.UniqueIdentifier = $"{Guid.NewGuid():N}";

                _context.Add(formHandler);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Form işleyici başarıyla oluşturuldu.";
                return RedirectToAction(nameof(Index));
            }
            return View(formHandler);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            var formHandler = await _context.FormHandlers.FindAsync(id);
            if (formHandler == null)
            {
                return NotFound();
            }
            return View(formHandler);

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, FormHandler formHandler)
        {
            if (id != formHandler.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // UniqueIdentifier'ın güncellenmesini engelle
                    _context.Entry(formHandler).Property(x => x.UniqueIdentifier).IsModified = false;
                    _context.Update(formHandler);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.FormHandlers.Any(e => e.Id == formHandler.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                TempData["SuccessMessage"] = "Form işleyici başarıyla güncellendi.";
                return RedirectToAction(nameof(Index));
            }
            return View(formHandler);
        }

        [HttpPost]
        public async Task<IActionResult> LoadData()
        {
            var draw = Request.Form["draw"].FirstOrDefault();
            var start = Request.Form["start"].FirstOrDefault();
            var length = Request.Form["length"].FirstOrDefault();
            var searchValue = Request.Form["search[value]"].FirstOrDefault();
            int pageSize = length != null ? Convert.ToInt32(length) : 0;
            int skip = start != null ? Convert.ToInt32(start) : 0;

            var data = _context.FormHandlers.AsQueryable();

            if (!string.IsNullOrEmpty(searchValue))
            {
                data = data.Where(m => m.Name.Contains(searchValue) || m.ActionTarget.Contains(searchValue));
            }

            int recordsTotal = await data.CountAsync();
            var result = await data.Skip(skip).Take(pageSize).ToListAsync();

            return Ok(new { draw, recordsFiltered = recordsTotal, recordsTotal, data = result });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var formHandler = await _context.FormHandlers.FindAsync(id);
            if (formHandler == null)
            {
                return Json(new { success = false, message = "İşleyici bulunamadı." });
            }
            _context.FormHandlers.Remove(formHandler);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Form işleyici başarıyla silindi." });
        }
    }
}
