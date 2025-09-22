using Dizgem.Models;
using System.Text.RegularExpressions;

namespace Dizgem.Services
{
    public class SeoService : ISeoService
    {
        public void EnsureSeoFields(Post post)
        {
            // Eğer SEO Başlığı kullanıcı tarafından girilmemişse, ana başlıktan oluştur.
            if (string.IsNullOrWhiteSpace(post.SeoTitle))
            {
                post.SeoTitle = post.Title;
            }

            // Eğer SEO Açıklaması kullanıcı tarafından girilmemişse, özet alanından oluştur.
            if (string.IsNullOrWhiteSpace(post.SeoDescription))
            {
                // Özet alanı da boşsa, ana içerikten oluşturmayı deneyebiliriz.
                // Bu örnekte, daha önce oluşturulmuş Excerpt alanını kullanıyoruz.
                post.SeoDescription = StripHtml(post.Excerpt);

                // Meta description genellikle 155-160 karakter civarında olmalıdır.
                if (post.SeoDescription.Length > 160)
                {
                    post.SeoDescription = post.SeoDescription.Substring(0, 157) + "...";
                }
            }
        }

        public void EnsureSeoFields(Page post)
        {
            // Eğer SEO Başlığı kullanıcı tarafından girilmemişse, ana başlıktan oluştur.
            if (string.IsNullOrWhiteSpace(post.SeoTitle))
            {
                post.SeoTitle = post.Title;
            }

            // Eğer SEO Açıklaması kullanıcı tarafından girilmemişse, özet alanından oluştur.
            if (string.IsNullOrWhiteSpace(post.SeoDescription))
            {
                // Özet alanı da boşsa, ana içerikten oluşturmayı deneyebiliriz.
                // Bu örnekte, daha önce oluşturulmuş Excerpt alanını kullanıyoruz.
                post.SeoDescription = StripHtml(post.Excerpt);

                // Meta description genellikle 155-160 karakter civarında olmalıdır.
                if (post.SeoDescription.Length > 160)
                {
                    post.SeoDescription = post.SeoDescription.Substring(0, 157) + "...";
                }
            }
        }

        /// <summary>
        /// Verilen bir metindeki HTML etiketlerini temizler.
        /// </summary>
        private string StripHtml(string input)
        {
            if (string.IsNullOrEmpty(input))
                return "";

            return Regex.Replace(input, "<.*?>", string.Empty);
        }
    }
}
