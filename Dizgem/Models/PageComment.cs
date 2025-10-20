namespace Dizgem.Models
{
    public class PageComment
    {
        public Guid PageId { get; set; }
        public Page Page { get; set; }

        public Guid CommentId { get; set; }
        public Comment Comment { get; set; }
    }
}
