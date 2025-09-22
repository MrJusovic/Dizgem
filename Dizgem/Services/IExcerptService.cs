namespace Dizgem.Services
{
    /// <summary>
    /// Verilen bir HTML içeriğinden metin özeti oluşturur.
    /// </summary>
    public interface IExcerptService
    {
        /// <summary>
        /// HTML içeriğini temizler, düz metne çevirir ve belirtilen uzunlukta bir özet oluşturur.
        /// </summary>
        /// <param name="htmlContent">Temizlenecek HTML metni.</param>
        /// <param name="maxLength">Özetin maksimum karakter uzunluğu.</param>
        /// <returns>Oluşturulan özet metni.</returns>
        string GenerateExcerpt(string htmlContent, int maxLength = 250);
    }
}
