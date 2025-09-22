using Dizgem.Services;
using Microsoft.Extensions.FileProviders;

namespace Dizgem.Middleware
{
    /// <summary>
    /// Her istekte aktif temanın statik dosyalarını (/assets) sunan özel bir middleware.
    /// </summary>
    public class ThemeStaticFileMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public ThemeStaticFileMiddleware(RequestDelegate next, IWebHostEnvironment hostingEnvironment, IServiceScopeFactory serviceScopeFactory)
        {
            _next = next;
            _hostingEnvironment = hostingEnvironment;
            _serviceScopeFactory = serviceScopeFactory;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Sadece /assets ile başlayan yolları hedef al
            if (context.Request.Path.StartsWithSegments("/assets"))
            {
                // Her istek için yeni bir scope oluşturarak IThemeService'i alıyoruz.
                // Bu, o anki aktif temanın doğru bir şekilde çözümlenmesini sağlar.
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var themeService = scope.ServiceProvider.GetRequiredService<IThemeService>();
                    var activeThemeName = await themeService.GetActiveThemeNameAsync();

                    if (!string.IsNullOrEmpty(activeThemeName))
                    {
                        var themeAssetPath = Path.Combine(_hostingEnvironment.ContentRootPath, "Themes", activeThemeName, "assets");
                        var fileProvider = new PhysicalFileProvider(themeAssetPath);

                        // İstenen dosyanın göreceli yolunu al (örneğin /css/style.css)
                        var relativePath = context.Request.Path.Value.Substring("/assets".Length);
                        var fileInfo = fileProvider.GetFileInfo(relativePath.TrimStart('/'));

                        if (fileInfo.Exists)
                        {
                            // Dosya varsa, tarayıcıya sun ve pipeline'ı sonlandır.
                            context.Response.ContentType = GetContentType(fileInfo.Name);
                            await context.Response.SendFileAsync(fileInfo);
                            return;
                        }
                    }
                }
            }

            // Eğer dosya bulunamadıysa veya yol /assets ile başlamıyorsa, sonraki middleware'e geç
            await _next(context);
        }

        // Dosya uzantısına göre basit bir MIME type belirleyici
        private static string GetContentType(string path)
        {
            var types = new Dictionary<string, string>
            {
                {".txt", "text/plain"},
                {".pdf", "application/pdf"},
                {".png", "image/png"},
                {".jpg", "image/jpeg"},
                {".jpeg", "image/jpeg"},
                {".gif", "image/gif"},
                {".svg", "image/svg+xml"},
                {".css", "text/css"},
                {".js", "application/javascript"}
            };
            var ext = Path.GetExtension(path).ToLowerInvariant();
            return types.GetValueOrDefault(ext, "application/octet-stream");
        }
    }

    // Middleware'i Program.cs'de kolayca çağırmak için bir genişletme metodu
    public static class ThemeStaticFileMiddlewareExtensions
    {
        public static IApplicationBuilder UseThemeStaticFiles(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ThemeStaticFileMiddleware>();
        }
    }
}
