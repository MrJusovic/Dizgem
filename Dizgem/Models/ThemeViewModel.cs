namespace Dizgem.Models
{
    /// <summary>
    /// Yönetim panelinde bir temayı temsil etmek için kullanılan model.
    /// theme.json dosyasından ve dosya sisteminden gelen bilgileri içerir.
    /// </summary>
    public class ThemeViewModel
    {
        /// <summary>
        /// Temanın klasör adı (Örn: "Default")
        /// </summary>
        public string DirectoryName { get; set; }

        // theme.json'dan gelen bilgiler
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string Version { get; set; }
        public string Author { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }

        /// <summary>
        /// Varsa, temanın ekran görüntüsünün web üzerindeki yolu (Örn: "/Themes/Default/screenshot.png")
        /// </summary>
        public string ScreenshotUrl { get; set; }
    }
}
