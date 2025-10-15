using Dizgem.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Dizgem.Areas.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class FormHandlersApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public FormHandlersApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /api/FormHandlersApi
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var handlers = await _context.FormHandlers
                .AsNoTracking()
                .Select(h => new { h.Name, h.UniqueIdentifier })
                .OrderBy(h => h.Name)
                .ToListAsync();

            // GrapesJS'in select options formatına uygun bir yapıya dönüştür
            var options = handlers.Select(h => new { value = h.UniqueIdentifier, name = h.Name }).ToList();

            return Ok(options);
        }
    }
}
