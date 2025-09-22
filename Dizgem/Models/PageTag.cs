namespace Dizgem.Models
{
    // Sayfalar ve Etiketler arasındaki çok-a-çok ilişkiyi kuran tablo
    public class PageTag
    {
        public Guid PageId { get; set; }
        public Page Page { get; set; }

        public Guid TagId { get; set; }
        public Tag Tag { get; set; }
    }
}
