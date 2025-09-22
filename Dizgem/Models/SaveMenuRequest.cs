namespace Dizgem.Models
{
    // AJAX ile menü kaydetme isteğinden gelen veriyi temsil eder.
    public class SaveMenuRequest
    {
        public string LocationId { get; set; }
        public List<MenuItemViewModel> MenuItems { get; set; }
    }
}
