using Microsoft.AspNetCore.Mvc.Razor;
using System.Diagnostics;

namespace Dizgem.Services
{
    public class ActiveThemeViewLocationExpander : IViewLocationExpander
    {
        private const string THEME_KEY = "theme";

        public IEnumerable<string> ExpandViewLocations(ViewLocationExpanderContext context, IEnumerable<string> viewLocations)
        {
            context.Values.TryGetValue(THEME_KEY, out var theme);

            // --- DEBUG ÇIKTILARI ---
            Debug.WriteLine($"--- View Arama Başladı: '{context.ViewName}', Controller: '{context.ControllerName}' ---");
            Debug.WriteLine($"Algılanan Area: '{context.AreaName ?? "YOK"}', Algılanan Tema: '{theme ?? "YOK"}'");

            if (string.IsNullOrEmpty(theme as string))
            {
                Debug.WriteLine("Tema bulunamadı, varsayılan yollar kullanılıyor.");
                return viewLocations;
            }

            var areaName = context.AreaName;
            IEnumerable<string> themeLocations;

            if (!string.IsNullOrEmpty(areaName))
            {
                // Area için çalışan ve şu an doğru olan kod
                themeLocations = new[]
                {
                $"/Areas/{areaName}/Themes/{theme}/Views/{{1}}/{{0}}.cshtml",
                $"/Areas/{areaName}/Themes/{theme}/Views/Shared/{{0}}.cshtml"
            };
            }
            else
            {
                // ANA SİTE İÇİN ÇALIŞAN VE MUHTEMELEN SORUNLU OLAN KOD
                themeLocations = new[]
                {
                $"/Themes/{theme}/{{1}}/{{0}}.cshtml",
                $"/Themes/{theme}/Shared/{{0}}.cshtml"
            };
            }

            var finalPaths = themeLocations.Concat(viewLocations).ToList();

            Debug.WriteLine("Arama Yapılacak Final Yollar:");
            foreach (var path in finalPaths)
            {
                Debug.WriteLine($" -> {path}");
            }
            Debug.WriteLine("--- View Arama Bitti ---");

            return finalPaths;
        }

        public void PopulateValues(ViewLocationExpanderContext context)
        {
            var themeService = context.ActionContext.HttpContext.RequestServices.GetService<IThemeService>();
            if (themeService != null)
            {
                // Aktif temayı al ve bağlama (context) ekle
                string activeTheme = themeService.GetActiveThemeNameAsync().Result;
                context.Values[THEME_KEY] = activeTheme;
            }
        }
    }
}
