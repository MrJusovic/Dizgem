using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Dizgem.Models
{
    public class User: IdentityUser<Guid>
    {
        // Ekranlarda gösterilecek isim
        public string DisplayName { get; set; }


        // Bu kullanıcının yayımladığı yazılar (navigasyon özelliği)
        public ICollection<Post> Posts { get; set; }
        public ICollection<Page> Pages { get; set; }
    }
}
