using Dizgem.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace Dizgem.Services
{
    /// <summary>
    /// Kullanıcı kimliği (cookie) oluşturulurken standart bilgilere ek olarak
    /// özel alanları (DisplayName gibi) eklemek için kullanılır.
    /// </summary>
    public class CustomUserClaimsPrincipalFactory : UserClaimsPrincipalFactory<User, IdentityRole<Guid>>
    {
        public CustomUserClaimsPrincipalFactory(
            UserManager<User> userManager,
            RoleManager<IdentityRole<Guid>> roleManager,
            IOptions<IdentityOptions> optionsAccessor)
            : base(userManager, roleManager, optionsAccessor)
        {
        }

        protected override async Task<ClaimsIdentity> GenerateClaimsAsync(User user)
        {
            // Önce varsayılan claim'leri (UserId, UserName vb.) oluştur.
            var identity = await base.GenerateClaimsAsync(user);

            // Şimdi kendi özel claim'lerimizi ekleyelim.
            if (!string.IsNullOrEmpty(user.DisplayName))
            {
                identity.AddClaim(new Claim("DisplayName", user.DisplayName));
            }

            if (!string.IsNullOrEmpty(user.color_theme_layout))
            {
                identity.AddClaim(new Claim("color_theme_layout", user.color_theme_layout));
            }

            if (!string.IsNullOrEmpty(user.theme_layout))
            {
                identity.AddClaim(new Claim("theme_layout", user.theme_layout));
            }

            if (!string.IsNullOrEmpty(user.page_layout))
            {
                identity.AddClaim(new Claim("page_layout", user.page_layout));
            }

            if (!string.IsNullOrEmpty(user.layout))
            {
                identity.AddClaim(new Claim("layout", user.layout));
            }

            if (!string.IsNullOrEmpty(user.sidebar_type))
            {
                identity.AddClaim(new Claim("sidebar_type", user.sidebar_type));
            }

            // Gelecekte eklenebilecek diğer özel claim'ler buraya gelebilir.
            // Örneğin: AvatarUrl, ProfileLink vb.

            return identity;
        }
    }
}
