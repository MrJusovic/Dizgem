using System.ComponentModel.DataAnnotations.Schema;

namespace Dizgem.Models
{
    public interface ISeoContent
    {
        string? SeoTitle { get; }
        string? SeoDescription { get; }
        string? SeoKeywords { get; }
        string? Slug { get; }
        public Guid? CoverPhotoMediaId { get; }
        public Media? CoverPhoto { get; }
    }
}
