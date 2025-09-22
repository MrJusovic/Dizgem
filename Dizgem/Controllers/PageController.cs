using Dizgem.Services;
using Microsoft.AspNetCore.Mvc;

namespace Dizgem.Controllers
{
    public class PageController : Controller
    {
        private readonly IPageService _pageService;

        public PageController(IPageService pageService)
        {
            _pageService = pageService;
        }

        // GET: /Post
        // GET: /archive/YYYY/MM
        public async Task<IActionResult> Index(int? year, int? month, int page = 1, int pageSize = 10)
        {
            if (page < 1)
            {
                page = 1;
            }

            // PostService'i kullanarak ilgili sayfadaki ve/veya arşivdeki yazı özetlerini
            // ve sayfalama bilgilerini içeren bir ViewModel iste.
            var viewModel = await _pageService.GetPublishedPagesAsync(page, pageSize, year, month);

            // Hazırlanan bu 'viewModel' nesnesini View'a gönder.
            // Bu sayede Index.cshtml dosyası, @Model.Posts, @Model.CurrentPage gibi
            // komutlarla hem yazı listesine hem de sayfa numaralarına erişebilir.
            return View(viewModel);
        }

        // GET: /slug-degeri
        public async Task<IActionResult> Detail(string slug)
        {
            // Slug'ın geçerli olup olmadığını kontrol et.
            if (string.IsNullOrEmpty(slug))
            {
                return BadRequest(); // Hatalı istek
            }

            // PostService'i kullanarak slug'a uygun yazıyı veritabanından iste.
            var post = await _pageService.GetPageBySlugAsync(slug);

            // Yazı bulunup bulunmadığını kontrol et.
            if (post == null)
            {
                return NotFound(); // Yazı yoksa 404 hatası göster.
            }

            // Bulunan 'post' nesnesini (yani Model'i) View'a gönder.
            return View(post);
        }
    }
}
