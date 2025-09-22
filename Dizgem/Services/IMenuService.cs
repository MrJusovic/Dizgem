using Dizgem.Models;
using Microsoft.AspNetCore.Html;

namespace Dizgem.Services
{
    public interface IMenuService
    {
        /// <summary>
        /// Tema dosyalarından okunmuş, mevcut tüm menü konumlarını getirir.
        /// </summary>
        Task<Dictionary<string, string>> GetAvailableMenuLocationsAsync();

        /// <summary>
        /// Yönetim paneli için gerekli tüm verileri (menüler, sayfalar, yazılar vb.) getirir.
        /// </summary>
        Task<MenuManagementViewModel> GetMenuManagementViewModelAsync();

        // Belirtilen menü konumuna ait menü öğelerini hiyerarşik olarak getirir.
        Task<List<MenuItemViewModel>> GetMenuItemsAsync(string locationId);

        // Gelen hiyerarşik menü yapısını veritabanına kaydeder.
        Task SaveMenuAsync(string locationId, List<MenuItemViewModel> menuItems);

        /// <summary>
        /// Belirtilen menü konumuna atanmış menüyü HTML olarak render eder.
        /// </summary>
        Task<IHtmlContent> RenderMenuAsync(string locationId);
    }

    // Arayüz ve Controller arasında veri taşımak için ViewModel'lar
    public class MenuManagementViewModel
    {
        public List<Menu> Menus { get; set; }
        public List<Page> Pages { get; set; }
        public List<Post> Posts { get; set; }
        public Dictionary<string, string> AvailableLocations { get; set; }
    }
}
