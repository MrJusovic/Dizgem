namespace Dizgem.Services
{
    public interface ISitemapService
    {
        Task<string> GenerateSitemapXmlAsync();
        Task<string> GenerateRobotsTxtAsync();
    }
}
