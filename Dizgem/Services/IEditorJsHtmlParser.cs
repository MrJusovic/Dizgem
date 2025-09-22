namespace Dizgem.Services
{
    public interface IEditorJsHtmlParser
    {
        /// <summary>
        /// Verilen Editor.js JSON'unu, tema dosyalarında gösterilecek güvenli HTML'e çevirir.
        /// </summary>
        string Parse(string editorJsJson);
        /// <summary>
        /// Verilen Editor.js JSON'u içindeki "raw" bloklarının içeriğini temizler.
        /// Bu, verinin veritabanına kaydedilmesinden önce yapılmalıdır.
        /// </summary>
        string SanitizeRawBlocks(string editorJsJson);
    }
}
