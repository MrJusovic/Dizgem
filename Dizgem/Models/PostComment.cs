namespace Dizgem.Models
{
    public class PostComment
    {
        public Guid PostId { get; set; }
        public Post Post { get; set; }

        public Guid CommentId { get; set; }
        public Comment Comment { get; set; }
    }
}
