using System.Text.RegularExpressions;
using System.Web;

namespace Dizgem.Services
{
    public class ExcerptService : IExcerptService
    {
        public string GenerateExcerpt(string htmlContent, int maxLength = 250)
        {
            if (string.IsNullOrEmpty(htmlContent))
            {
                return string.Empty;
            }

            // 1. HTML etiketlerini temizle
            string plainText = Regex.Replace(HttpUtility.HtmlDecode(htmlContent), "<.*?>", string.Empty);

            // 2. &nbsp; gibi HTML entity'lerini düz karaktere çevir
            plainText = HttpUtility.HtmlDecode(plainText);

            // 3. Satır sonlarını ve fazla boşlukları tek boşluğa indir
            plainText = Regex.Replace(plainText, @"\s+", " ").Trim();

            // 4. Metni belirtilen uzunluğa kısalt
            if (plainText.Length <= maxLength)
            {
                return plainText;
            }

            // Kelimeyi ortadan bölmemek için son boşluğu bul
            int lastSpace = plainText.LastIndexOf(' ', maxLength);
            if (lastSpace > 0)
            {
                return plainText.Substring(0, lastSpace) + "...";
            }

            // Boşluk bulunamazsa, metni doğrudan kes
            return plainText.Substring(0, maxLength) + "...";
        }
    }
}
