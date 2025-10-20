using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Dizgem.Models
{
    public class User: IdentityUser<Guid>
    {
        // Ekranlarda gösterilecek isim
        public string DisplayName { get; set; }

        public string color_theme_layout { get; set; } = "light";
        public string theme_layout { get; set; } = "Blue_Theme";
        public string page_layout { get; set; } = "vertical";
        public string layout { get; set; } = "boxed";
        public string sidebar_type { get; set; } = "full";


        // Bu kullanıcının yayımladığı yazılar (navigasyon özelliği)
        public ICollection<Post> Posts { get; set; }
        public ICollection<Page> Pages { get; set; }
    }
}
