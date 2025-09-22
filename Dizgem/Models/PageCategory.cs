using System.ComponentModel.DataAnnotations;

namespace Dizgem.Models
{
    /// <summary>
    /// Page ve Category arasındaki çok-a-çok ilişkiyi temsil eden bağlantı tablosu.
    /// </summary>
    public class PageCategory
    {
        [Required]
        public Guid PageId { get; set; }
        public Page Page { get; set; }

        [Required]
        public Guid CategoryId { get; set; }
        public Category Category { get; set; }

        /// <summary>
        /// Bu kategorinin, sayfa için birincil kategori olup olmadığını belirtir.
        /// </summary>
        public bool IsPrimary { get; set; }
    }
}
