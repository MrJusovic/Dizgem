namespace Dizgem.Models
{
    // Yazılar ve Kategoriler arasındaki çok-a-çok ilişkiyi kuran tablo
    public class PostCategory
    {
        public Guid PostId { get; set; }
        public Post Post { get; set; }

        public Guid CategoryId { get; set; }
        public Category Category { get; set; }

        // Bu kategorinin, yazı için ana kategori olup olmadığını belirtir.
        public bool IsPrimary { get; set; }
    }
}
