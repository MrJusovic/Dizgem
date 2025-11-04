using Dizgem.Models;

namespace Dizgem.Services
{
    public interface IThemeEditorService
    {
        /// <summary>
        /// /Themes klasöründeki tüm temaları ve dosyalarını hiyerarşik bir yapıda döndürür.
        /// </summary>
        /// <param name="activeThemeName">Varsayılan olarak açık gösterilecek temanın adı.</param>
        /// <returns>Tema ağacını temsil eden düğümlerin listesi.</returns>
        List<ThemeFileNodeViewModel> GetThemeTree(string activeThemeName);

        /// <summary>
        /// Belirtilen yoldaki dosyanın içeriğini okur.
        /// </summary>
        /// <param name="relativePath">/Themes klasörüne göreli dosya yolu.</param>
        /// <returns>Dosyanın metin içeriği.</returns>
        Task<string> GetFileContentAsync(string relativePath);

        /// <summary>
        /// Belirtilen yoldaki dosyaya yeni içeriği yazar.
        /// </summary>
        /// <param name="relativePath">/Themes klasörüne göreli dosya yolu.</param>
        /// <param name="content">Dosyaya yazılacak yeni içerik.</param>
        /// <returns>İşlemin başarı durumunu ve bir mesajı içeren bir Tuple.</returns>
        Task<(bool Success, string Message)> SaveFileContentAsync(string relativePath, string content);


        Task<(bool Success, string Message)> CreateFileAsync(string relativePath);
        Task<(bool Success, string Message)> CreateFolderAsync(string relativePath);

        Task<(bool Success, string Message)> RenameNodeAsync(string oldRelativePath, string newRelativePath);
    }
}
