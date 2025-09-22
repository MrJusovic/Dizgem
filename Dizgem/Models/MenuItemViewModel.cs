namespace Dizgem.Models
{
    // Hem sunucudan arayüze menü yapısını göndermek hem de arayüzden sunucuya
    // güncellenmiş yapıyı göndermek için kullanılan hiyerarşik model.
    public class MenuItemViewModel
    {
        public Guid Id { get; set; }
        public string Label { get; set; }
        public string Url { get; set; }
        public string CssClass { get; set; }
        public int Order { get; set; }
        public List<MenuItemViewModel> Children { get; set; } = new List<MenuItemViewModel>();
    }
}
