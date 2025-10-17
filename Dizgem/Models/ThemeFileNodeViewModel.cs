namespace Dizgem.Models
{
    public class ThemeFileNodeViewModel
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public bool IsDirectory { get; set; }
        public bool IsOpen { get; set; } // Ağaç görünümünde varsayılan olarak açık mı?
        public List<ThemeFileNodeViewModel> Children { get; set; } = new List<ThemeFileNodeViewModel>();
    }
}
