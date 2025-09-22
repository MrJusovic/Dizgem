using Dizgem.Data;
using Dizgem.Models;
using Microsoft.AspNetCore.Html;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;

namespace Dizgem.Services
{
    public class MenuService : IMenuService
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IThemeService _themeService;

        public MenuService(ApplicationDbContext context, IWebHostEnvironment env, IThemeService themeService)
        {
            _context = context;
            _env = env;
            _themeService = themeService;
        }

        public async Task<Dictionary<string, string>> GetAvailableMenuLocationsAsync()
        {
            var themeName = await _themeService.GetActiveThemeNameAsync();
            var themeJsonPath = Path.Combine(_env.ContentRootPath, "Themes", themeName, "theme.json");

            if (!File.Exists(themeJsonPath))
                return new Dictionary<string, string>();

            var jsonContent = await File.ReadAllTextAsync(themeJsonPath);
            var themeInfo = JsonSerializer.Deserialize<ThemeInfo>(jsonContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return themeInfo?.MenuLocations ?? new Dictionary<string, string>();
        }

        public async Task<MenuManagementViewModel> GetMenuManagementViewModelAsync()
        {
            return new MenuManagementViewModel
            {
                Menus = await _context.Menus.Include(m => m.MenuItems).ToListAsync(),
                Pages = await _context.Pages.Where(p => p.IsPublished).OrderBy(p => p.Title).ToListAsync(),
                Posts = await _context.Posts.Where(p => p.IsPublished).OrderByDescending(p => p.PublishedDate).Take(20).ToListAsync(),
                AvailableLocations = await GetAvailableMenuLocationsAsync()
            };
        }

        public async Task<List<MenuItemViewModel>> GetMenuItemsAsync(string locationId)
        {
            var menu = await _context.Menus.AsNoTracking()
                .FirstOrDefaultAsync(m => m.LocationId == locationId);

            if (menu == null)
            {
                return new List<MenuItemViewModel>();
            }

            var allItems = await _context.MenuItems.AsNoTracking()
                .Where(i => i.MenuId == menu.Id)
                .OrderBy(i => i.Order)
                .ToListAsync();

            var itemLookup = allItems.ToDictionary(
                item => item.Id,
                item => new MenuItemViewModel
                {
                    Id = item.Id,
                    Label = item.Label,
                    Url = item.Url,
                    CssClass = item.CssClass,
                    Order = item.Order,
                });

            var rootItems = new List<MenuItemViewModel>();
            foreach (var dbItem in allItems)
            {
                var vmItem = itemLookup[dbItem.Id];
                if (dbItem.ParentId.HasValue && itemLookup.ContainsKey(dbItem.ParentId.Value))
                {
                    itemLookup[dbItem.ParentId.Value].Children.Add(vmItem);
                }
                else
                {
                    rootItems.Add(vmItem);
                }
            }
            return rootItems;
        }

        public async Task SaveMenuAsync(string locationId, List<MenuItemViewModel> menuItems)
        {
            // 1. İlgili menüyü bul veya oluştur
            var menu = await _context.Menus.FirstOrDefaultAsync(m => m.LocationId == locationId);
            if (menu == null)
            {
                menu = new Menu { Id = Guid.NewGuid(), LocationId = locationId, Name = locationId };
                _context.Menus.Add(menu);
                await _context.SaveChangesAsync(); // ID'nin oluşması için kaydet
            }

            // 2. Bu menüye ait eski tüm öğeleri sil
            var existingItems = _context.MenuItems.Where(i => i.MenuId == menu.Id);
            _context.MenuItems.RemoveRange(existingItems);
            await _context.SaveChangesAsync();

            // 3. Yeni menü yapısını hiyerarşik olarak ekle
            if (menuItems != null && menuItems.Any())
            {
                await AddMenuItemsRecursively(menuItems, menu.Id, null);
            }

            await _context.SaveChangesAsync();
        }

        private async Task AddMenuItemsRecursively(List<MenuItemViewModel> items, Guid menuId, Guid? parentId)
        {
            foreach (var item in items.OrderBy(i => i.Order))
            {
                var newItem = new MenuItem
                {
                    Id = Guid.NewGuid(),
                    MenuId = menuId,
                    ParentId = parentId,
                    Title = item.Label,
                    Label = item.Label,
                    Url = item.Url,
                    CssClass = item.CssClass,
                    Order = item.Order
                };
                _context.MenuItems.Add(newItem);

                if (item.Children != null && item.Children.Any())
                {
                    await AddMenuItemsRecursively(item.Children, menuId, newItem.Id);
                }
            }
        }

        public async Task<IHtmlContent> RenderMenuAsync(string locationId)
        {
            var menu = await _context.Menus
                .Include(m => m.MenuItems)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.LocationId == locationId);

            if (menu == null)
                return HtmlString.Empty;

            var topLevelItems = menu.MenuItems.Where(i => i.ParentId == null).OrderBy(i => i.Order).ToList();

            var sb = new StringBuilder();
            sb.Append($"<ul id='{menu.Id}' class='menu custom-style navbar-nav me-auto mb-2 mb-sm-0'>"); // Menü'ye ait ID ve class eklenebilir

            foreach (var item in topLevelItems)
            {
                BuildMenuItemHtml(sb, item, menu.MenuItems);
            }

            sb.Append("</ul>");
            return new HtmlString(sb.ToString());
        }

        private void BuildMenuItemHtml(StringBuilder sb, MenuItem item, ICollection<MenuItem> allItems)
        {
            var children = allItems.Where(i => i.ParentId == item.Id).OrderBy(i => i.Order).ToList();
            var hasChildren = children.Any();

            sb.Append($"<li id='{item.ElementId}' class='nav-item {(hasChildren ? "menu-item-has-children dropdown" : "")} {item.CssClass}'>");
            sb.Append($"<a href='{item.Url}' {(hasChildren ? "data-bs-toggle=\"dropdown\" aria-expanded=\"false\"": "class=\"nav-link\"")}>{item.Title}</a>");

            if (hasChildren)
            {
                sb.Append("<ul class='sub-menu dropdown-menu'>");
                foreach (var child in children)
                {
                    BuildMenuItemHtml(sb, child, allItems);
                }
                sb.Append("</ul>");
            }

            sb.Append("</li>");
        }
    }

    // theme.json dosyasını deserialize etmek için yardımcı sınıf
    public class ThemeInfo
    {
        public Dictionary<string, string> MenuLocations { get; set; }
    }
}
