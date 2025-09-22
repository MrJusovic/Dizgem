using Dizgem.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace Dizgem.Services
{
    public class SlugService : ISlugService
    {
        private readonly ApplicationDbContext _context;

        public SlugService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<string> GenerateUniqueSlugAsync(string title, string? slug, Guid? entityId, string entityType = "post")
        {
            // 1. Adım: Temel Slug'ı Oluştur
            string baseSlug = CreateSlug(string.IsNullOrWhiteSpace(slug) ? title : slug);

            // 2. Adım: Benzersizliği Kontrol Et ve Gerekirse Sonuna Ek Yap
            string finalSlug = baseSlug;
            int counter = 1;

            while (await IsSlugExistsAsync(finalSlug, entityId, entityType))
            {
                finalSlug = $"{baseSlug}-{counter}";
                counter++;
            }

            return finalSlug;
        }

        private async Task<bool> IsSlugExistsAsync(string slug, Guid? entityId, string entityType)
        {
            // Gelen entityType'a göre doğru DbSet üzerinde sorgu yap
            return entityType.ToLower() switch
            {
                "post" => await _context.Posts.AnyAsync(p => p.Slug == slug && p.Id != entityId),
                "category" => await _context.Categories.AnyAsync(c => c.Slug == slug && c.Id != entityId),
                "tag" => await _context.Tags.AnyAsync(t => t.Slug == slug && t.Id != entityId),
                _ => false, // Tanımsız bir tip gelirse false dön
            };
        }

        private string CreateSlug(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "";

            // Türkçe karakterleri ve diğer özel karakterleri değiştir
            text = text.ToLowerInvariant();
            text = text.Replace('ı', 'i');
            text = text.Replace('ğ', 'g');
            text = text.Replace('ü', 'u');
            text = text.Replace('ş', 's');
            text = text.Replace('ö', 'o');
            text = text.Replace('ç', 'c');

            // Geçersiz karakterleri kaldır
            text = Regex.Replace(text, @"[^a-z0-9\s-]", "");
            // Boşlukları tire ile değiştir
            text = Regex.Replace(text, @"\s+", "-").Trim();
            // Birden fazla tireyi tek tireye indir
            text = Regex.Replace(text, @"-+", "-");
            // Başta veya sonda olabilecek tireleri kaldır
            text = text.Trim('-');

            return text;
        }
    }
}
