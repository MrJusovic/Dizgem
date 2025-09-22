using Dizgem.Data;
using Dizgem.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace Dizgem.Services
{
    public class PageService : IPageService
    {
        private readonly ApplicationDbContext _context;
        private readonly ISlugService _slugService;
        private readonly ISeoService _seoService;
        private readonly IExcerptService _excerptService;
        private readonly IEditorJsHtmlParser _htmlParser;


        public PageService(ApplicationDbContext context, ISlugService slugService, ISeoService seoService, IExcerptService excerptService, IEditorJsHtmlParser htmlParser)
        {
            _context = context;
            _slugService = slugService;
            _seoService = seoService;
            _excerptService = excerptService;
            _htmlParser = htmlParser;
        }

        // --- Ön Yüz Metotları ---

        public async Task<PostIndexViewModel> GetPublishedPagesAsync(int page, int pageSize, int? year = null, int? month = null)
        {
            var query = _context.Pages
                .Where(p => p.IsPublished);

            if (year.HasValue)
            {
                query = query.Where(p => p.PublishedDate.Year == year.Value);
            }
            if (month.HasValue)
            {
                query = query.Where(p => p.PublishedDate.Month == month.Value);
            }

            var totalPosts = await query.CountAsync();
            var pages = await query.OrderByDescending(p => p.PublishedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new PostSummaryViewModel
                {
                    Title = p.Title,
                    Slug = p.Slug,
                    Excerpt = p.Excerpt,
                    CoverPhotoUrl = p.CoverPhotoUrl,
                    PublishedDate = p.PublishedDate,
                    AuthorDisplayName = p.Author.DisplayName,
                    PrimaryCategoryName = p.PageCategories.FirstOrDefault(pc => pc.IsPrimary).Category.Name
                }).ToListAsync();

            return new PostIndexViewModel
            {
                Posts = pages,
                TotalPages = (int)Math.Ceiling(totalPosts / (double)pageSize),
                CurrentPage = page,
                Year = year,
                Month = month
            };
        }

        public async Task<Page> GetPageBySlugAsync(string slug)
        {
            return await _context.Pages
                .Include(p => p.Author)
                .Include(p => p.PageCategories).ThenInclude(pc => pc.Category)
                .Include(p => p.PageTags).ThenInclude(pt => pt.Tag)
                .FirstOrDefaultAsync(p => p.IsPublished && p.Slug == slug);
        }

        public async Task<IEnumerable<PostArchiveItemViewModel>> GetPageArchiveAsync()
        {
            return await _context.Pages
                .Where(p => p.IsPublished)
                .GroupBy(p => new { p.PublishedDate.Year, p.PublishedDate.Month })
                .Select(g => new PostArchiveItemViewModel
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    PostCount = g.Count()
                })
                .OrderByDescending(a => a.Year).ThenByDescending(a => a.Month)
                .ToListAsync();
        }

        // --- Yönetim Paneli Metotları ---

        public async Task<PostEditViewModel<Page>> GetPageForEditAsync(Guid? pageId)
        {
            var viewModel = new PostEditViewModel<Page>()
            {
                AllCategories = await _context.Categories.OrderBy(c => c.Name).ToListAsync(),
                AllTags = await _context.Tags.OrderBy(t => t.Name).ToListAsync()

            };


            if (pageId.HasValue && pageId.Value != Guid.Empty) // Edit modu
            {
                viewModel.Post = await _context.Pages
                    .Include(p => p.PageCategories)
                    .Include(p => p.PageTags).ThenInclude(pt => pt.Tag)
                    .AsNoTracking() // Takip gereksiz, performansı artırır.
                    .FirstOrDefaultAsync(p => p.Id == pageId.Value);

                if (viewModel.Post != null)
                {
                    viewModel.SelectedCategoryIds = viewModel.Post.PageCategories.Select(pc => pc.CategoryId).ToList();
                    viewModel.PrimaryCategoryId = viewModel.Post.PageCategories.FirstOrDefault(pc => pc.IsPrimary)?.CategoryId;
                    viewModel.TagsString = string.Join(",", viewModel.Post.PageTags.Select(pt => pt.Tag.Name));
                }
            }

            return viewModel;
        }

        public async Task CreatePageAsync(PostEditViewModel<Page> model, Guid authorId)
        {
            var page = model.Post;

            page.Id = Guid.NewGuid();
            page.AuthorId = authorId;
            page.PublishedDate = DateTime.Now;

            await ProcessPostData(page, model);

            _context.Pages.Add(page);
            await _context.SaveChangesAsync();

            await UpdatePostRelations(page.Id, model);
        }

        public async Task UpdatePageAsync(PostEditViewModel<Page> model)
        {
            var page = model.Post;

            await ProcessPostData(page, model);

            _context.Pages.Update(page);
            await UpdatePostRelations(page.Id, model);

            await _context.SaveChangesAsync();
        }

        public async Task<bool> DeletePageAsync(Guid pageId)
        {
            var page = await _context.Pages.FindAsync(pageId);
            if (page == null)
            {
                return false;
            }

            _context.Pages.Remove(page);
            await _context.SaveChangesAsync();
            return true;
        }

        private async Task ProcessPostData(Page page, PostEditViewModel<Page> model)
        {
            // Veritabanına kaydedilecek olan ham JSON'u temizliyoruz.
            page.ContentJson = _htmlParser.SanitizeRawBlocks(page.ContentJson);

            page.Content = _htmlParser.Parse(page.ContentJson);

            // Excerpt alanını otomatik olarak oluştur
            var getExcerpt = string.IsNullOrWhiteSpace(page.Excerpt) ? page.Content : page.Excerpt;
            page.Excerpt = _excerptService.GenerateExcerpt(getExcerpt);

            page.Slug = await _slugService.GenerateUniqueSlugAsync(page.Title, page.Slug, page.Id);
            _seoService.EnsureSeoFields(page);
        }

        private async Task UpdatePostRelations(Guid pageId, PostEditViewModel<Page> model)
        {
            // Kategorileri güncelle
            var existingCategories = _context.PostCategories.Where(pc => pc.PostId == pageId);
            _context.PostCategories.RemoveRange(existingCategories);

            if (model.SelectedCategoryIds != null && model.SelectedCategoryIds.Any())
            {
                var relations = model.SelectedCategoryIds.Select(catId => new PostCategory
                {
                    PostId = pageId,
                    CategoryId = catId,
                    IsPrimary = (catId == model.PrimaryCategoryId)
                });
                await _context.PostCategories.AddRangeAsync(relations);
            }

            // Etiketleri güncelle
            var existingTags = _context.PostTags.Where(pt => pt.PostId == pageId);
            _context.PostTags.RemoveRange(existingTags);

            if (!string.IsNullOrWhiteSpace(model.TagsString))
            {
                var tagNames = model.TagsString.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                                     .Select(t => t.ToLowerInvariant()).Distinct();

                foreach (var tagName in tagNames)
                {
                    var tag = await _context.Tags.FirstOrDefaultAsync(t => t.Name.ToLower() == tagName);
                    if (tag == null)
                    {
                        tag = new Tag { Name = tagName, Slug = await _slugService.GenerateUniqueSlugAsync(tagName, null, null, "tag") };
                        _context.Tags.Add(tag);
                        // Yeni tag'i hemen kaydet ki ID'si oluşsun.
                        await _context.SaveChangesAsync();
                    }
                    _context.PostTags.Add(new PostTag { PostId = pageId, TagId = tag.Id });
                }
            }
            await _context.SaveChangesAsync();
        }
    }
}
