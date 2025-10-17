using Dizgem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dizgem.Areas.Dizgem.Controllers
{
    [Area("Dizgem")]
    [Authorize(Roles = "Admin")]
    public class ThemeEditorController : Controller
    {
        private readonly IThemeEditorService _themeEditorService;
        private readonly IThemeService _themeService;

        public ThemeEditorController(IThemeEditorService themeEditorService, IThemeService themeService)
        {
            _themeEditorService = themeEditorService;
            _themeService = themeService;
        }

        public async Task<IActionResult> Index()
        {
            var activeTheme = await _themeService.GetActiveThemeNameAsync();
            var model = _themeEditorService.GetThemeTree(activeTheme);
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> GetFileContent(string path)
        {
            try
            {
                var content = await _themeEditorService.GetFileContentAsync(path);
                return Ok(new { success = true, content });
            }
            catch (System.Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveFileContent([FromBody] FileSaveRequest model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = "Geçersiz istek." });
            }

            var (success, message) = await _themeEditorService.SaveFileContentAsync(model.Path, model.Content);

            if (success)
            {
                return Ok(new { success = true, message });
            }
            else
            {
                return BadRequest(new { success = false, message });
            }
        }
    }

    public class FileSaveRequest
    {
        public string Path { get; set; }
        public string Content { get; set; }
    }
}
