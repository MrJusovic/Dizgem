using System.Security.Claims;

/// <summary>
/// User (ClaimsPrincipal) nesnesini, projemize özel claim'lere
/// kolayca erişmek için genişletir.
/// </summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Giriş yapmış kullanıcının DisplayName claim'ini döndürür.
    /// </summary>
    public static string GetDisplayName(this ClaimsPrincipal principal)
    {
        if (principal == null)
        {
            return string.Empty;
        }

        // Önce "DisplayName" claim'ini arar.
        var displayNameClaim = principal.FindFirst("DisplayName");

        // Bulursa onun değerini, bulamazsa varsayılan kullanıcı adını (UserName) döndürür.
        return displayNameClaim?.Value ?? principal.Identity?.Name ?? "";
    }
}