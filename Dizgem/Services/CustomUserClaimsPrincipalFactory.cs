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

            // Gelecekte eklenebilecek diğer özel claim'ler buraya gelebilir.
            // Örneğin: AvatarUrl, ProfileLink vb.

            return identity;
        }
    }
}
