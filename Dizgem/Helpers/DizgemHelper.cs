using Dizgem.Models;
using Dizgem.Services;
using Microsoft.AspNetCore.Html;
using System.Text;
using System.Web;

namespace Dizgem.Helpers
{
    /// <summary>
    /// Tema geliştiricilerinin sık kullanılan işlemleri kolayca yapabilmesi için
    /// statik yardımcı metotlar içerir.
    /// </summary>
    public static class DizgemHelper
    {

        // --- 1. RAZOR TARAFINDAN ÇAĞRILACAK YENİ GENEL METOT ---
        // Bu metot, Model'i 'dynamic' olarak kabul eder ve tip kontrolünü kendi yapar.
        public static IHtmlContent CreateMetaTags(ISettingsService settingsService, HttpRequest request, dynamic? model = null)
        {
            // Gelen 'model'in, SEO için gerekli özelliklere sahip olup olmadığını kontrol et.
            // 'model' bir Post veya Page nesnesi ise (yani ISeoContent implement ediyorsa), 
            // onu 'seoContent' değişkenine ata.
            if (model is ISeoContent seoContent)
            {
                // Eğer uyumluysa, asıl işi yapan private metoda bu nesneyi gönder.
                return CreateMetaTagsInternal(settingsService, request, seoContent);
            }
            else
            {
                // Eğer model uyumsuzsa veya null ise, private metoda 'content' parametresini null olarak gönder.
                // Bu durumda T tipini açıkça belirtmemiz gerekir.
                return CreateMetaTagsInternal<ISeoContent>(settingsService, request, null);
            }
        }

        /// <summary>
        /// Mevcut sayfa için standart ve sosyal medya SEO meta etiketlerini oluşturur.
        /// </summary>
        /// <param name="settingsService">Ayarlar servisi.</param>
        /// <param name="request">Mevcut HTTP isteği.</param>
        /// <param name="post">Eğer bir yazı detay sayfası ise ilgili Post nesnesi.</param>
        private static IHtmlContent CreateMetaTagsInternal<T>(ISettingsService settingsService, HttpRequest request, T? content = null) where T : class, ISeoContent // T, ISeoContent arayüzünü implement etmeli
        {
            var settings = settingsService.Current;
            var sb = new StringBuilder();

            // content nesnesi artık Page veya Post olabilir, ISeoContent üzerinden erişim yapıyoruz.

            // --- Temel Değerleri Belirleme ---
            string title = !string.IsNullOrWhiteSpace(content?.SeoTitle) ? content.SeoTitle : settings.SiteTitle;
            string description = !string.IsNullOrWhiteSpace(content?.SeoDescription) ? content.SeoDescription : settings.SiteDescription;
            string keywords = content?.SeoKeywords ?? "";

            // Tam ve mutlak URL oluşturma (Sayfa tipine göre farklı rota belirlenebilir)
            string routePrefix = (content is Post) ? "/post/" : (content is Page) ? "/page/" : "";

            var currentUrl = new Uri(
                new Uri($"{request.Scheme}://{request.Host}"),
                content != null ? $"{routePrefix}{content.Slug}" : request.Path
            ).ToString();

            // Kullanılacak resmi belirleme:
            string? imageUrl = content?.CoverPhotoMediaId != Guid.Empty && content?.CoverPhotoMediaId != null ? content?.CoverPhoto?.UrlFull : settings.SiteImageUrl;
            if (!string.IsNullOrWhiteSpace(imageUrl) && !imageUrl.StartsWith("http"))
            {
                imageUrl = new Uri(new Uri($"{request.Scheme}://{request.Host}"), imageUrl).ToString();
            }

            // --- Standart Meta Etiketleri ---
            sb.AppendLine($"<title>{HttpUtility.HtmlEncode(title)}</title>");
            sb.AppendLine($"<meta name=\"description\" content=\"{HttpUtility.HtmlEncode(description)}\" />");
            if (!string.IsNullOrEmpty(keywords))
            {
                sb.AppendLine($"<meta name=\"keywords\" content=\"{HttpUtility.HtmlEncode(keywords)}\" />");
            }

            // Favicon mantığı (değişmedi)
            if (!string.IsNullOrEmpty(settings.FaviconUrl))
            {
                string? FaviconUrl = settings.FaviconUrl;
                if (!string.IsNullOrWhiteSpace(FaviconUrl) && !FaviconUrl.StartsWith("http"))
                {
                    FaviconUrl = new Uri(new Uri($"{request.Scheme}://{request.Host}"), FaviconUrl).ToString();
                }

                sb.AppendLine($"<link rel=\"icon\" href=\"{FaviconUrl}\" />");
            }

            // SEO için Canonical URL
            sb.AppendLine($"<link rel=\"canonical\" href=\"{currentUrl}\" />");

            sb.AppendLine();

            // --- Open Graph Meta Etiketleri (Facebook, LinkedIn vb.) ---
            sb.AppendLine($"");
            // content is Post kontrolü ile og:type dinamikleştirildi
            sb.AppendLine($"<meta property=\"og:type\" content=\"{(content is Post ? "article" : "website")}\" />");
            sb.AppendLine($"<meta property=\"og:url\" content=\"{currentUrl}\" />");
            sb.AppendLine($"<meta property=\"og:title\" content=\"{HttpUtility.HtmlEncode(title)}\" />");
            sb.AppendLine($"<meta property=\"og:description\" content=\"{HttpUtility.HtmlEncode(description)}\" />");
            if (!string.IsNullOrWhiteSpace(imageUrl))
            {
                sb.AppendLine($"<meta property=\"og:image\" content=\"{imageUrl}\" />");
            }
            sb.AppendLine($"<meta property=\"og:site_name\" content=\"{HttpUtility.HtmlEncode(settings.SiteTitle)}\" />");

            sb.AppendLine();

            // --- Twitter Card Meta Etiketleri ---
            sb.AppendLine($"");
            sb.AppendLine($"<meta property=\"twitter:card\" content=\"summary_large_image\" />");
            sb.AppendLine($"<meta property=\"twitter:url\" content=\"{currentUrl}\" />");
            sb.AppendLine($"<meta property=\"twitter:title\" content=\"{HttpUtility.HtmlEncode(title)}\" />");
            sb.AppendLine($"<meta property=\"twitter:description\" content=\"{HttpUtility.HtmlEncode(description)}\" />");
            if (!string.IsNullOrWhiteSpace(imageUrl))
            {
                sb.AppendLine($"<meta property=\"twitter:image\" content=\"{imageUrl}\" />");
            }
            if (!string.IsNullOrWhiteSpace(settings.TwitterHandle))
            {
                sb.AppendLine($"<meta name=\"twitter:creator\" content=\"@{settings.TwitterHandle}\" />");
                sb.AppendLine($"<meta name=\"twitter:site\" content=\"@{settings.TwitterHandle}\" />");
            }

            return new HtmlString(sb.ToString());
        }
    }
}
