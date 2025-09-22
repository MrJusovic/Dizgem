using Dizgem.Data;
using Dizgem.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Dizgem.Services
{
    public class PostService : IPostService
    {
        private readonly ApplicationDbContext _context;
        private readonly ISlugService _slugService;
        private readonly ISeoService _seoService;
        private readonly IExcerptService _excerptService;
        private readonly IEditorJsHtmlParser _htmlParser;


        public PostService(ApplicationDbContext context, ISlugService slugService, ISeoService seoService, IExcerptService excerptService, IEditorJsHtmlParser htmlParser)
        {
            _context = context;
            _slugService = slugService;
            _seoService = seoService;
            _excerptService = excerptService;
            _htmlParser = htmlParser;
        }

        // --- Ön Yüz Metotları ---

        public async Task<PostIndexViewModel> GetPublishedPostsAsync(int page, int pageSize, int? year = null, int? month = null)
        {
            var query = _context.Posts
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
            var posts = await query.OrderByDescending(p => p.PublishedDate)
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
                    PrimaryCategoryName = p.PostCategories.FirstOrDefault(pc => pc.IsPrimary).Category.Name
                }).ToListAsync();

            return new PostIndexViewModel
            {
                Posts = posts,
                TotalPages = (int)Math.Ceiling(totalPosts / (double)pageSize),
                CurrentPage = page,
                Year = year,
                Month = month
            };
        }

        public async Task<Post> GetPostBySlugAsync(string slug)
        {
            return await _context.Posts
                .Include(p => p.Author)
                .Include(p => p.PostCategories).ThenInclude(pc => pc.Category)
                .Include(p => p.PostTags).ThenInclude(pt => pt.Tag)
                .FirstOrDefaultAsync(p => p.IsPublished && p.Slug == slug);
        }

        public async Task<IEnumerable<PostArchiveItemViewModel>> GetPostArchiveAsync()
        {
            return await _context.Posts
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

        public async Task<PostEditViewModel<Post>> GetPostForEditAsync(Guid? postId)
        {
            var viewModel = new PostEditViewModel<Post>()
            {
                AllCategories = await _context.Categories.OrderBy(c => c.Name).ToListAsync(),
                AllTags = await _context.Tags.OrderBy(t => t.Name).ToListAsync()

            };


            if (postId.HasValue && postId.Value != Guid.Empty) // Edit modu
            {
                viewModel.Post = await _context.Posts
                    .Include(p => p.PostCategories)
                    .Include(p => p.PostTags).ThenInclude(pt => pt.Tag)
                    .AsNoTracking() // Takip gereksiz, performansı artırır.
                    .FirstOrDefaultAsync(p => p.Id == postId.Value);

                if (viewModel.Post != null)
                {
                    viewModel.SelectedCategoryIds = viewModel.Post.PostCategories.Select(pc => pc.CategoryId).ToList();
                    viewModel.PrimaryCategoryId = viewModel.Post.PostCategories.FirstOrDefault(pc => pc.IsPrimary)?.CategoryId;
                    viewModel.TagsString = string.Join(",", viewModel.Post.PostTags.Select(pt => pt.Tag.Name));
                }
            }


            return viewModel;
        }

        public async Task CreatePostAsync(PostEditViewModel<Post> model, Guid authorId)
        {
            var post = model.Post;

            post.Id = Guid.NewGuid();
            post.AuthorId = authorId;
            post.PublishedDate = DateTime.Now;

            await ProcessPostData(post, model);

            _context.Posts.Add(post);
            await _context.SaveChangesAsync();

            await UpdatePostRelations(post.Id, model);
        }

        public async Task UpdatePostAsync(PostEditViewModel<Post> model)
        {
            var post = model.Post;

            await ProcessPostData(post, model);

            _context.Posts.Update(post);
            await UpdatePostRelations(post.Id, model);

            await _context.SaveChangesAsync();
        }

        public async Task<bool> DeletePostAsync(Guid postId)
        {
            var post = await _context.Posts.FindAsync(postId);
            if (post == null)
            {
                return false;
            }

            _context.Posts.Remove(post);
            await _context.SaveChangesAsync();
            return true;
        }

        private async Task ProcessPostData(Post post, PostEditViewModel<Post> model)
        {
            // Veritabanına kaydedilecek olan ham JSON'u temizliyoruz.
            post.ContentJson = _htmlParser.SanitizeRawBlocks(post.ContentJson);

            post.Content = _htmlParser.Parse(post.ContentJson);

            // Excerpt alanını otomatik olarak oluştur
            var getExcerpt = string.IsNullOrWhiteSpace(post.Excerpt) ? post.Content : post.Excerpt;
            post.Excerpt = _excerptService.GenerateExcerpt(getExcerpt);

            post.Slug = await _slugService.GenerateUniqueSlugAsync(post.Title, post.Slug, post.Id);
            _seoService.EnsureSeoFields(post);
        }

        private async Task UpdatePostRelations(Guid postId, PostEditViewModel<Post> model)
        {
            // Kategorileri güncelle
            var existingCategories = _context.PostCategories.Where(pc => pc.PostId == postId);
            _context.PostCategories.RemoveRange(existingCategories);

            if (model.SelectedCategoryIds != null && model.SelectedCategoryIds.Any())
            {
                var relations = model.SelectedCategoryIds.Select(catId => new PostCategory
                {
                    PostId = postId,
                    CategoryId = catId,
                    IsPrimary = (catId == model.PrimaryCategoryId)
                });
                await _context.PostCategories.AddRangeAsync(relations);
            }

            // Etiketleri güncelle
            var existingTags = _context.PostTags.Where(pt => pt.PostId == postId);
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
                    _context.PostTags.Add(new PostTag { PostId = postId, TagId = tag.Id });
                }
            }
            await _context.SaveChangesAsync();
        }


    }
}
