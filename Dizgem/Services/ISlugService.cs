namespace Dizgem.Services
{
    /// <summary>
    /// Verilen bir metinden benzersiz bir URL metni (slug) oluşturur.
    /// </summary>
    public interface ISlugService
    {
        /// <summary>
        /// Verilen bir başlığa veya mevcut slug'a göre benzersiz bir slug oluşturur.
        /// </summary>
        /// <param name="title">Slug oluşturmak için kullanılacak ana başlık.</param>
        /// <param name="slug">Kullanıcı tarafından girilmiş veya mevcut olan slug. Boş ise başlıktan oluşturulur.</param>
        /// <param name="entityId">Düzenlenen varlığın ID'si. Kendi slug'ını kontrol etmemek için kullanılır.</param>
        /// <param name="entityType">Slug'ın hangi tabloda (post, category, tag) kontrol edileceğini belirtir. Varsayılan 'post'tur.</param>
        /// <returns>Veritabanında benzersiz olan bir slug metni.</returns>
        Task<string> GenerateUniqueSlugAsync(string title, string? slug, Guid? entityId, string entityType = "post");
    }
}
