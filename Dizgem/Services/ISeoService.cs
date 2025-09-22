using Dizgem.Models;

namespace Dizgem.Services
{
    public interface ISeoService
    {
        /// <summary>
        /// Bir Post nesnesinin SEO alanlarının (SeoTitle, SeoDescription) boş olup olmadığını kontrol eder
        /// ve boş ise otomatik olarak doldurur.
        /// </summary>
        /// <param name="post">İşlem görecek Post nesnesi.</param>
        void EnsureSeoFields(Post post);
        void EnsureSeoFields(Page post);
    }
}
