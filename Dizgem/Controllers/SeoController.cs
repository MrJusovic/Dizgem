using Dizgem.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace Dizgem.Controllers
{
    public class SeoController : Controller
    {
        private readonly ISitemapService _sitemapService;

        public SeoController(ISitemapService sitemapService)
        {
            _sitemapService = sitemapService;
        }

        [Route("/sitemap.xml")]
        public async Task<IActionResult> Sitemap()
        {
            var sitemapContent = await _sitemapService.GenerateSitemapXmlAsync();
            if (string.IsNullOrEmpty(sitemapContent))
            {
                return NotFound();
            }
            return Content(sitemapContent, "application/xml", Encoding.UTF8);
        }

        [Route("/robots.txt")]
        [Route("/llms.txt")] // İsteğiniz üzerine llms.txt'yi de destekliyor
        public async Task<IActionResult> Robots()
        {
            var robotsContent = await _sitemapService.GenerateRobotsTxtAsync();
            return Content(robotsContent, "text/plain", Encoding.UTF8);
        }
    }
}
