using Dizgem.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Dizgem.Areas.Dizgem.Controllers
{
    [Area("Dizgem")]
    [Authorize(Roles = "Admin")]
    public class FormSubmissionsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public FormSubmissionsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /DizgemAdmin/FormSubmissions?formHandlerId={id}
        public async Task<IActionResult> Index(Guid formHandlerId)
        {
            var formHandler = await _context.FormHandlers.FindAsync(formHandlerId);
            if (formHandler == null)
            {
                return NotFound();
            }

            ViewData["FormHandlerName"] = formHandler.Name;
            ViewData["FormHandlerId"] = formHandler.Id;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> LoadSubmissionsData(Guid formHandlerId)
        {
            var draw = Request.Form["draw"].FirstOrDefault();
            var start = Request.Form["start"].FirstOrDefault();
            var length = Request.Form["length"].FirstOrDefault();
            var searchValue = Request.Form["search[value]"].FirstOrDefault();

            int pageSize = length != null ? Convert.ToInt32(length) : 0;
            int skip = start != null ? Convert.ToInt32(start) : 0;

            var query = _context.FormSubmissions
                .Where(fs => fs.FormHandlerId == formHandlerId);

            if (!string.IsNullOrEmpty(searchValue))
            {
                query = query.Where(fs => fs.DataJson.Contains(searchValue) || fs.SubmissionDate.ToString().Contains(searchValue));
            }

            int recordsTotal = await query.CountAsync();
            var data = await query.OrderByDescending(fs => fs.SubmissionDate)
                                  .Skip(skip)
                                  .Take(pageSize)
                                  .ToListAsync();

            var jsonData = new
            {
                draw = draw,
                recordsFiltered = recordsTotal,
                recordsTotal = recordsTotal,
                data = data.Select(fs => new
                {
                    fs.Id,
                    SubmissionDate = fs.SubmissionDate.ToString("dd.MM.yyyy HH:mm:ss"),
                    // Önizleme için JSON verisinden kısa bir metin alalım
                    DataPreview = fs.DataJson.Length > 100 ? fs.DataJson.Substring(0, 100) + "..." : fs.DataJson
                })
            };

            return Ok(jsonData);
        }

        // GET: /DizgemAdmin/FormSubmissions/Detail/{id}
        public async Task<IActionResult> Detail(Guid id)
        {
            var submission = await _context.FormSubmissions
                .Include(fs => fs.FormHandler)
                .FirstOrDefaultAsync(fs => fs.Id == id);

            if (submission == null)
            {
                return NotFound();
            }

            // JSON verisini daha okunabilir bir formata (Dictionary) çevir
            var dataDictionary = JsonSerializer.Deserialize<Dictionary<string, string>>(submission.DataJson);
            ViewData["FormData"] = dataDictionary;

            return View(submission);
        }
    }
}
