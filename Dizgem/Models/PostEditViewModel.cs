using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Dizgem.Models
{
    /// <summary>
    /// Yazı oluşturma ve düzenleme sayfası için veri modelini temsil eder.
    /// Yazının kendisini, kategori ve etiket listelerini ve seçilen değerleri içerir.
    /// </summary>
    public class PostEditViewModel<T> 
    {
        /// <summary>
        /// Oluşturulan veya düzenlenen Post nesnesi.
        /// </summary>
        public T Post { get; set; }

        // --- Kategori Yönetimi ---

        /// <summary>
        /// Formda gösterilecek tüm mevcut kategorileri tutar.
        /// </summary>
        public List<Category> AllCategories { get; set; } = new();

        /// <summary>
        /// Formda gösterilecek tüm mevcut etiketleri tutar.
        /// </summary>
        public List<Tag> AllTags { get; set; } = new();

        /// <summary>
        /// Kullanıcı tarafından seçilen kategorilerin ID'lerini tutar.
        /// </summary>
        public List<Guid> SelectedCategoryIds { get; set; } = new();

        /// <summary>
        /// Kullanıcı tarafından ana kategori olarak işaretlenen kategorinin ID'sini tutar.
        /// Ana kategori zorunlu olmadığı için nullable (Guid?) olarak tanımlanmıştır.
        /// </summary>
        [Display(Name = "Ana Kategori")]
        public Guid? PrimaryCategoryId { get; set; }


        // --- Etiket Yönetimi ---

        /// <summary>
        /// Yazıya ait etiketleri tek bir, virgülle ayrılmış metin olarak temsil eder.
        /// Bu yaklaşım, kullanıcıların mevcut etiketleri eklemesine veya sadece yazarak anında 
        /// yeni etiketler oluşturmasına olanak tanıyarak harika bir kullanıcı deneyimi sunar.
        /// PostService katmanı, bu metni işlemeyi, mevcut etiketleri isme göre bulmayı,
        /// gerekirse yeni etiketler oluşturmayı ve ilişkileri ID'leri üzerinden kaydetmeyi üstlenir.
        /// </summary>
        [Display(Name = "Etiketler (virgülle ayırın)")]
        public string TagsString { get; set; }
    }
}
