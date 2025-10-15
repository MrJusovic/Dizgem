namespace Dizgem.Models
{
    public interface ISeoContent
    {
        string? SeoTitle { get; }
        string? SeoDescription { get; }
        string? SeoKeywords { get; }
        string? Slug { get; }
        string? CoverPhotoUrl { get; }
    }
}
