using Dizgem.Models;

namespace Dizgem.Services
{
    public class ThemeEditorService : IThemeEditorService
    {
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly string _themesRootPath;

        public ThemeEditorService(IWebHostEnvironment hostingEnvironment)
        {
            _hostingEnvironment = hostingEnvironment;
            _themesRootPath = Path.Combine(_hostingEnvironment.ContentRootPath, "Themes");
        }

        public List<ThemeFileNodeViewModel> GetThemeTree(string activeThemeName)
        {
            var root = new List<ThemeFileNodeViewModel>();
            if (!Directory.Exists(_themesRootPath))
            {
                return root;
            }

            foreach (var dir in Directory.GetDirectories(_themesRootPath))
            {
                var dirInfo = new DirectoryInfo(dir);
                var isNodeOpen = string.Equals(dirInfo.Name, activeThemeName, System.StringComparison.OrdinalIgnoreCase);

                var themeNode = new ThemeFileNodeViewModel
                {
                    Name = dirInfo.Name,
                    Path = dirInfo.Name,
                    IsDirectory = true,
                    IsOpen = isNodeOpen,
                    Children = GetDirectoryNodes(dir, dirInfo.Name)
                };
                root.Add(themeNode);
            }
            return root;
        }

        private List<ThemeFileNodeViewModel> GetDirectoryNodes(string path, string relativePath)
        {
            var nodes = new List<ThemeFileNodeViewModel>();
            var directoryInfo = new DirectoryInfo(path);

            foreach (var dir in directoryInfo.GetDirectories())
            {
                var currentRelativePath = Path.Combine(relativePath, dir.Name);
                nodes.Add(new ThemeFileNodeViewModel
                {
                    Name = dir.Name,
                    Path = currentRelativePath,
                    IsDirectory = true,
                    Children = GetDirectoryNodes(dir.FullName, currentRelativePath)
                });
            }

            foreach (var file in directoryInfo.GetFiles())
            {
                nodes.Add(new ThemeFileNodeViewModel
                {
                    Name = file.Name,
                    Path = Path.Combine(relativePath, file.Name),
                    IsDirectory = false
                });
            }

            return nodes.OrderBy(n => !n.IsDirectory).ThenBy(n => n.Name).ToList();
        }

        public async Task<string> GetFileContentAsync(string relativePath)
        {
            var fullPath = GetSafeFullPath(relativePath);
            if (fullPath == null || !File.Exists(fullPath))
            {
                throw new FileNotFoundException("Dosya bulunamadı veya erişim yetkiniz yok.");
            }
            return await File.ReadAllTextAsync(fullPath);
        }

        public async Task<(bool Success, string Message)> SaveFileContentAsync(string relativePath, string content)
        {
            var fullPath = GetSafeFullPath(relativePath);
            if (fullPath == null)
            {
                return (false, "Geçersiz dosya yolu. Kaydetme işlemi iptal edildi.");
            }

            try
            {
                await File.WriteAllTextAsync(fullPath, content);
                return (true, $"{Path.GetFileName(relativePath)} dosyası başarıyla kaydedildi.");
            }
            catch (System.Exception ex)
            {
                return (false, $"Dosya kaydedilirken bir hata oluştu: {ex.Message}");
            }
        }

        private string GetSafeFullPath(string relativePath)
        {
            // Güvenlik: Path Traversal saldırılarını önlemek için yolu normalize et
            var fullPath = Path.GetFullPath(Path.Combine(_themesRootPath, relativePath));

            // Kullanıcının /Themes klasörünün dışına çıkmadığından emin ol
            if (!fullPath.StartsWith(_themesRootPath))
            {
                return null;
            }
            return fullPath;
        }
    }
}
