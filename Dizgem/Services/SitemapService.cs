using Dizgem.Data;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml.Linq;

namespace Dizgem.Services
{
    public class SitemapService : ISitemapService
    {
        private readonly ApplicationDbContext _context;
        private readonly ISettingsService _settingsService; // Ayarları okumak için
        private readonly IExcerptService _excerptService; // Ayarları okumak için

        public SitemapService(ApplicationDbContext context, ISettingsService settingsService, IExcerptService excerptService)
        {
            _context = context;
            _settingsService = settingsService;
            _excerptService = excerptService;
        }

        public async Task<string> GenerateSitemapXmlAsync()
        {
            var settings = _settingsService.Current;
            var siteUrl = settings?.SiteUrl;

            if (string.IsNullOrWhiteSpace(siteUrl))
            {
                // Site URL ayarlanmamışsa, sitemap oluşturulamaz.
                return string.Empty;
            }

            XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
            var urlset = new XElement(ns + "urlset");

            // Yayınlanmış Yazıları Ekle
            var posts = await _context.Posts
                .Where(p => p.IsPublished)
                .OrderByDescending(p => p.PublishedDate)
                .ToListAsync();

            foreach (var post in posts)
            {
                var urlElement = new XElement(ns + "url",
                    new XElement(ns + "loc", $"{siteUrl.TrimEnd('/')}/post/{post.Slug}"),
                    new XElement(ns + "lastmod", post.PublishedDate.ToString("yyyy-MM-dd")),
                    new XElement(ns + "changefreq", "weekly"),
                    new XElement(ns + "priority", "0.8")
                );
                urlset.Add(urlElement);
            }

            // Yayınlanmış Sayfaları Ekle
            var pages = await _context.Pages
                .Where(p => p.IsPublished)
                .OrderByDescending(p => p.PublishedDate)
                .ToListAsync();

            foreach (var page in pages)
            {
                var urlElement = new XElement(ns + "url",
                    new XElement(ns + "loc", $"{siteUrl.TrimEnd('/')}/{page.Slug}"),
                    new XElement(ns + "lastmod", page.PublishedDate.ToString("yyyy-MM-dd")),
                    new XElement(ns + "changefreq", "monthly"),
                    new XElement(ns + "priority", "0.5")
                );
                urlset.Add(urlElement);
            }

            var sitemap = new XDocument(new XDeclaration("1.0", "utf-8", "yes"), urlset);
            return sitemap.ToString();
        }

        public async Task<string> GenerateRobotsTxtAsync()
        {
            var settings = _settingsService.Current;
            var siteUrl = settings?.SiteUrl;

            var sb = new StringBuilder();

            // --- Geleneksel Arama Motorları ve Diğer Botlar İçin Genel Kurallar ---
            sb.AppendLine("# General rules for all crawlers");
            sb.AppendLine("User-agent: *");
            sb.AppendLine("Allow: /");
            sb.AppendLine("Disallow: /Dizgem/");
            sb.AppendLine("Disallow: /Install/");
            sb.AppendLine("Disallow: /api/");
            sb.AppendLine();

            // --- Popüler AI ve LLM Botları İçin Açık İzin Kuralları ---
            // Bu, sitenizin AI modelleri tarafından taranmasına ve endekslenmesine
            // izin verme niyetinizi net bir şekilde belirtir.
            sb.AppendLine("# Explicit allow rules for common AI crawlers");

            sb.AppendLine("User-agent: Google-Extended");
            sb.AppendLine("Allow: /");
            sb.AppendLine();

            sb.AppendLine("User-agent: GPTBot");
            sb.AppendLine("Allow: /");
            sb.AppendLine();

            sb.AppendLine("User-agent: CCBot");
            sb.AppendLine("Allow: /");
            sb.AppendLine();

            // --- Site Haritası Konumu ---
            if (!string.IsNullOrWhiteSpace(siteUrl))
            {
                sb.AppendLine($"Sitemap: {siteUrl.TrimEnd('/')}/sitemap.xml");
            }

            return sb.ToString();
        }

        public Task<string> GenerateLlmsTxtAsync()
        {
            var settings = _settingsService.Current;
            string baseUrl = settings.SiteUrl;
            var sb = new StringBuilder();
            sb.AppendLine("# Bu, sitemizin içeriğini LLM'lerin anlaması için bir rehberdir.");
            sb.AppendLine("# Daha fazla bilgi için: https://llmstxt.org");
            sb.AppendLine();
            sb.AppendLine($"content: {baseUrl}/llms-content.md");

            return Task.FromResult(sb.ToString());
        }

        public async Task<string> GenerateLlmsContentMarkdownAsync()
        {
            var sb = new StringBuilder();
            var settings = _settingsService.Current;

            // Ana Başlık ve Açıklama
            sb.AppendLine($"# {settings.SiteTitle}");
            sb.AppendLine();
            sb.AppendLine(settings.SiteDescription);
            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();

            // Sayfaları Ekle
            sb.AppendLine("## Sayfalar");
            sb.AppendLine();
            var pages = await _context.Pages.Where(p => p.IsPublished).ToListAsync();
            foreach (var page in pages)
            {
                sb.AppendLine($"### Sayfa: {page.Title}");
                sb.AppendLine();
                // HTML'i temizle ve metin olarak ekle
                var content = Regex.Replace(HttpUtility.HtmlDecode(page.Content), "<.*?>", " ");
                sb.AppendLine(content?.CleanWhitespace());
                sb.AppendLine();
            }

            // Yazıları Ekle
            sb.AppendLine("## Yazılar");
            sb.AppendLine();
            var posts = await _context.Posts.Where(p => p.IsPublished).OrderByDescending(p => p.PublishedDate).ToListAsync();
            foreach (var post in posts)
            {
                sb.AppendLine($"### Yazı: {post.Title}");
                sb.AppendLine($"(Yayınlanma Tarihi: {post.PublishedDate:dd MMMM yyyy})");
                sb.AppendLine();
                var content = Regex.Replace(HttpUtility.HtmlDecode(post.RenderedContent.ToString()), "<.*?>", " ");
                sb.AppendLine(content?.CleanWhitespace());
                sb.AppendLine();
            }

            return sb.ToString();
        }
    }
}
