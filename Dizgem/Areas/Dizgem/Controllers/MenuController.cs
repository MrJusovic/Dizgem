using Dizgem.Models;
using Dizgem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Dizgem.Areas.Dizgem.Controllers
{
    [Area("Dizgem")]
    [Authorize]
    public class MenuController : Controller
    {
        private readonly IMenuService _menuService;

        public MenuController(IMenuService menuService)
        {
            _menuService = menuService;
        }

        public async Task<IActionResult> Index()
        {
            var model = await _menuService.GetMenuManagementViewModelAsync();
            return View(model);
        }

        public async Task<IActionResult> GetMenu(string locationId)
        {
            if (string.IsNullOrEmpty(locationId))
            {
                return BadRequest();
            }
            var menuItems = await _menuService.GetMenuItemsAsync(locationId);
            return Ok(menuItems);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save([FromBody] SaveMenuRequest model)
        {
            // [FromBody] ve [ValidateAntiForgeryToken] birlikte kullanıldığında 
            // ModelState.IsValid güvenilir bir kontrol yöntemi değildir.
            // Bunun yerine, gelen modelin temel özelliklerini manuel olarak kontrol ediyoruz.
            if (model == null || string.IsNullOrWhiteSpace(model.LocationId) || model.MenuItems == null)
            {
                return BadRequest(new { success = false, message = "Geçersiz veya eksik menü verisi." });
            }

            try
            {
                await _menuService.SaveMenuAsync(model.LocationId, model.MenuItems);
                return Ok(new { success = true, message = "Menü başarıyla kaydedildi." });
            }
            catch (Exception)
            {
                // Gerçek bir uygulamada burada hatayı loglamak önemlidir.
                return StatusCode(500, new { success = false, message = "Menü kaydedilirken bir sunucu hatası oluştu." });
            }
        }
    }
}
