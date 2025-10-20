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

    public static string GetColorThemeLayout(this ClaimsPrincipal principal)
    {
        if (principal == null)
        {
            return string.Empty;
        }

        // Önce "DisplayName" claim'ini arar.
        var displayNameClaim = principal.FindFirst("color_theme_layout");

        // Bulursa onun değerini, bulamazsa varsayılan kullanıcı adını (UserName) döndürür.
        return displayNameClaim?.Value ?? "light";
    }

    public static string GetThemeLayout(this ClaimsPrincipal principal)
    {
        if (principal == null)
        {
            return string.Empty;
        }

        // Önce "DisplayName" claim'ini arar.
        var displayNameClaim = principal.FindFirst("theme_layout");

        // Bulursa onun değerini, bulamazsa varsayılan kullanıcı adını (UserName) döndürür.
        return displayNameClaim?.Value ?? "Blue_Theme";
    }

    public static string GetPageLayout(this ClaimsPrincipal principal)
    {
        if (principal == null)
        {
            return string.Empty;
        }

        // Önce "DisplayName" claim'ini arar.
        var displayNameClaim = principal.FindFirst("page_layout");

        // Bulursa onun değerini, bulamazsa varsayılan kullanıcı adını (UserName) döndürür.
        return displayNameClaim?.Value ?? "vertical";
    }

    public static string GetLayout(this ClaimsPrincipal principal)
    {
        if (principal == null)
        {
            return string.Empty;
        }

        // Önce "DisplayName" claim'ini arar.
        var displayNameClaim = principal.FindFirst("layout");

        // Bulursa onun değerini, bulamazsa varsayılan kullanıcı adını (UserName) döndürür.
        return displayNameClaim?.Value ?? "boxed";
    }

    public static string GetSidebarType(this ClaimsPrincipal principal)
    {
        if (principal == null)
        {
            return string.Empty;
        }

        // Önce "DisplayName" claim'ini arar.
        var displayNameClaim = principal.FindFirst("sidebar_type");

        // Bulursa onun değerini, bulamazsa varsayılan kullanıcı adını (UserName) döndürür.
        return displayNameClaim?.Value ?? "full";
    }

    public static string GetEmail(this ClaimsPrincipal principal)
    {
        if (principal == null)
        {
            return string.Empty;
        }

        // Önce "DisplayName" claim'ini arar.
        var displayNameClaim = principal.FindFirst(x=> x.Type.Contains("emailaddress"));

        // Bulursa onun değerini, bulamazsa varsayılan kullanıcı adını (UserName) döndürür.
        return displayNameClaim?.Value ?? principal.Identity?.Name ?? "";
    }
}