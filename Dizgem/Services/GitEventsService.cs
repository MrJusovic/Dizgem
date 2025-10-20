using Azure;
using Dizgem.Models;
using Microsoft.IdentityModel.Logging;
using System.Net.Http;
using System.Text.Json.Nodes;

namespace Dizgem.Services
{
    public class GitEventsService : IGitEventsService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IWebHostEnvironment _env;

        // --- ÖNEMLİ: BU ALANLARI KENDİ GITHUB BİLGİLERİNİZLE DEĞİŞTİRİN ---
        private const string GitHubOwner = "mrjusovic";
        private const string GitHubRepo = "Dizgem";
        // --------------------------------------------------------------------

        public GitEventsService(IHttpClientFactory httpClientFactory, IWebHostEnvironment env)
        {
            _httpClientFactory = httpClientFactory;
            _env = env;
        }

        public async Task<(bool, IEnumerable<GitEventsViewModel>)> GetEvents()
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Add("User-Agent", "Dizgem-CMS-Updater");

                var url = $"https://api.github.com/repos/{GitHubOwner}/{GitHubRepo}/events";
                var response = await client.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    LogHelper.LogWarning($"Bilgiler Alınamadı\nStatus Code : {response.StatusCode}\nResponse : {await response.Content.ReadAsStringAsync()}");
                    return (false, null);
                }

                var jsonString = await response.Content.ReadAsStringAsync();
                var eventsInfo = JsonArray.Parse(jsonString).AsArray();
                List<GitEventsViewModel> gitEventsViews = new List<GitEventsViewModel>();
                foreach (var item in eventsInfo)
                {
                    if (item?["type"].ToString() == "ReleaseEvent")
                    {
                        gitEventsViews.Add(new GitEventsViewModel()
                        {
                            url = item?["payload"]?["release"]?["url"].ToString(),
                            html_url = item?["payload"]?["release"]?["html_url"].ToString(),
                            id = (long)item?["payload"]?["release"]?["id"],
                            tag_name = item?["payload"]?["release"]?["tag_name"].ToString(),
                            name = item?["payload"]?["release"]?["name"].ToString(),
                            body = item?["payload"]?["release"]?["body"].ToString(),
                            short_description_html = item?["payload"]?["release"]?["short_description_html"].ToString(),
                            is_short_description_html_truncated = (bool)item?["payload"]?["release"]?["is_short_description_html_truncated"],
                            created_at = item?["created_at"].ToString()

                        });
                    }
                }

                return (true, gitEventsViews);
            }
            catch (Exception ex)
            {
                LogHelper.LogWarning($"Bilgiler alınırken bir hata meydana geldi. {ex.Message}");
                return (false, null);
            }
        }
    }
}
