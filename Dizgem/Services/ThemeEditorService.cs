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

        // --- GÜVENLİK YARDIMCI METODU ---
        /// <summary>
        /// Göreceli yolu tam bir fiziksel yola dönüştürür ve güvenlik kontrolü yapar (Path Traversal).
        /// </summary>
        private string ValidateAndResolvePath(string relativePath, bool allowRootTheme = false)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                throw new ArgumentException("Yol boş olamaz.");
            }

            // .. ve : gibi geçersiz karakterleri temizle
            var cleanRelativePath = relativePath.Replace("..", "").Replace(":", "");
            var fullPath = Path.GetFullPath(Path.Combine(_themesRootPath, cleanRelativePath));

            // Path Traversal saldırılarını engelleme
            if (!fullPath.StartsWith(_themesRootPath, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Geçersiz dosya yolu. Tema klasörü dışına erişim engellendi.");
            }

            // allowRootTheme false ise (varsayılan), temanın ana klasöründe işlem yapılmasını engelle
            if (!allowRootTheme)
            {
                var relativePathParts = relativePath.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
                if (relativePathParts.Length <= 1)
                {
                    throw new InvalidOperationException("Aktif temanın ana klasörü üzerinde işlem yapılamaz.");
                }
            }

            return fullPath;
        }

        // --- YENİ EKLENEN METOTLARIN İMPLEMENTASYONU ---

        public async Task<(bool Success, string Message)> CreateFileAsync(string relativePath)
        {
            try
            {
                var fullPath = ValidateAndResolvePath(relativePath);
                if (File.Exists(fullPath))
                {
                    return (false, "Bu isimde bir dosya zaten mevcut.");
                }

                await File.WriteAllTextAsync(fullPath, ""); // Boş bir dosya oluştur
                return (true, $"'{Path.GetFileName(relativePath)}' dosyası başarıyla oluşturuldu.");
            }
            catch (Exception ex)
            {
                return (false, $"Dosya oluşturulurken bir hata oluştu: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> CreateFolderAsync(string relativePath)
        {
            try
            {
                var fullPath = ValidateAndResolvePath(relativePath);
                if (Directory.Exists(fullPath))
                {
                    return (false, "Bu isimde bir klasör zaten mevcut.");
                }

                Directory.CreateDirectory(fullPath);
                return (true, $"'{Path.GetFileName(relativePath)}' klasörü başarıyla oluşturuldu.");
            }
            catch (Exception ex)
            {
                return (false, $"Klasör oluşturulurken bir hata oluştu: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> RenameNodeAsync(string oldRelativePath, string newRelativePath)
        {
            // Task.CompletedTask'i yalnızca async derleyicisini memnun etmek için kullanıyoruz,
            // asıl I/O işlemleri senkron. Gerçek bir senaryoda bu işlemler de asenkron olabilir.
            await Task.CompletedTask;
            try
            {
                var fullOldPath = ValidateAndResolvePath(oldRelativePath);
                var fullNewPath = ValidateAndResolvePath(newRelativePath);

                if (!File.Exists(fullOldPath) && !Directory.Exists(fullOldPath))
                {
                    return (false, "İsim değiştirilecek dosya veya klasör bulunamadı.");
                }
                if (File.Exists(fullNewPath) || Directory.Exists(fullNewPath))
                {
                    return (false, "Bu isimde bir dosya veya klasör zaten mevcut.");
                }

                if (File.Exists(fullOldPath))
                {
                    File.Move(fullOldPath, fullNewPath);
                }
                else
                {
                    Directory.Move(fullOldPath, fullNewPath);
                }

                return (true, "İsim başarıyla değiştirildi.");
            }
            catch (Exception ex)
            {
                return (false, $"İsim değiştirilirken bir hata oluştu: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> DeleteNodeAsync(string relativePath)
        {
            await Task.CompletedTask; // Async uyumluluğu için
            try
            {
                var fullPath = ValidateAndResolvePath(relativePath);

                if (!File.Exists(fullPath) && !Directory.Exists(fullPath))
                {
                    return (false, "Silinecek dosya veya klasör bulunamadı.");
                }

                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                }
                else
                {
                    // true parametresi, klasörün içindekilerle birlikte (recursive) silinmesini sağlar.
                    Directory.Delete(fullPath, true);
                }

                return (true, $"'{Path.GetFileName(relativePath)}' başarıyla silindi.");
            }
            catch (Exception ex)
            {
                return (false, $"Silme işlemi sırasında bir hata oluştu: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> UploadFileAsync(string relativeDirectoryPath, IFormFile file)
        {
            try
            {
                // Yüklenecek hedef klasörün yolunu doğrula (kök tema klasörüne yüklemeye izin ver)
                var fullDirectoryPath = ValidateAndResolvePath(relativeDirectoryPath, allowRootTheme: true);
                if (!Directory.Exists(fullDirectoryPath))
                {
                    return (false, "Dosyanın yükleneceği klasör bulunamadı.");
                }

                // Hedef dosya yolunu oluştur
                var targetFilePath = Path.Combine(fullDirectoryPath, file.FileName);

                if (File.Exists(targetFilePath))
                {
                    return (false, $"'{file.FileName}' isimli dosya bu konumda zaten mevcut.");
                }

                // Dosyayı diske kaydet
                await using (var stream = new FileStream(targetFilePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                return (true, $"'{file.FileName}' dosyası başarıyla yüklendi.");
            }
            catch (Exception ex)
            {
                return (false, $"Dosya yüklenirken bir hata oluştu: {ex.Message}");
            }
        }
    }
}
