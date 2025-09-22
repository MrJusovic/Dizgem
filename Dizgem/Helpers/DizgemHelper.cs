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
        /// <summary>
        /// Mevcut sayfa için standart ve sosyal medya SEO meta etiketlerini oluşturur.
        /// </summary>
        /// <param name="settingsService">Ayarlar servisi.</param>
        /// <param name="request">Mevcut HTTP isteği.</param>
        /// <param name="post">Eğer bir yazı detay sayfası ise ilgili Post nesnesi.</param>
        public static IHtmlContent CreateMetaTags(ISettingsService settingsService, HttpRequest request, Post? post = null)
        {
            var settings = settingsService.Current;
            var sb = new StringBuilder();

            // --- Temel Değerleri Belirleme ---
            string title = !string.IsNullOrWhiteSpace(post?.SeoTitle) ? post.SeoTitle : settings.SiteTitle;
            string description = !string.IsNullOrWhiteSpace(post?.SeoDescription) ? post.SeoDescription : settings.SiteDescription;
            string keywords = post?.SeoKeywords ?? "";

            // Tam ve mutlak URL oluşturma
            var currentUrl = new Uri(new Uri($"{request.Scheme}://{request.Host}"), post != null ? $"/post/{post.Slug}" : request.Path).ToString();

            // Kullanılacak resmi belirleme: Önce yazının kapak fotoğrafı, yoksa sitenin varsayılan resmi
            string? imageUrl = post?.CoverPhotoUrl ?? settings.SiteImageUrl;
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
            if (!string.IsNullOrEmpty(settings.FaviconUrl))
            {
                string? FaviconUrl = settings.FaviconUrl;
                if (!string.IsNullOrWhiteSpace(FaviconUrl) && !FaviconUrl.StartsWith("http"))
                {
                    FaviconUrl = new Uri(new Uri($"{request.Scheme}://{request.Host}"), FaviconUrl).ToString();
                }

                sb.AppendLine($"<link rel=\"icon\" href=\"{FaviconUrl}\" />");
            }
            // SEO için Canonical URL ekleniyor
            sb.AppendLine($"<link rel=\"canonical\" href=\"{currentUrl}\" />");

            sb.AppendLine(); // Okunabilirlik için boşluk

            // --- Open Graph Meta Etiketleri (Facebook, LinkedIn vb.) ---
            sb.AppendLine($"<!-- Open Graph / Facebook -->");
            sb.AppendLine($"<meta property=\"og:type\" content=\"{(post != null ? "article" : "website")}\" />");
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
            sb.AppendLine($"<!-- Twitter -->");
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
