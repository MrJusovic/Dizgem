namespace Dizgem.Models
{
    // Yazılar ve Etiketler arasındaki çok-a-çok ilişkiyi kuran tablo
    public class PostTag
    {
        public Guid PostId { get; set; }
        public Post Post { get; set; }

        public Guid TagId { get; set; }
        public Tag Tag { get; set; }
    }
}
