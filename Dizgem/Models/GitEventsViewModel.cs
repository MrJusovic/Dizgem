namespace Dizgem.Models
{
    public class GitEventsViewModel
    {
        public string url { get; set; } = "";
        public string html_url { get; set; } = "";
        public long id { get; set; }
        public string tag_name { get; set; } = "";
        public string name { get; set; } = "";
        public string created_at { get; set; } = "";
        public string body { get; set; } = "";
        public string short_description_html { get; set; } = "";
        public bool is_short_description_html_truncated { get; set; } = false;
    }

    //public class GitReleaseObj
    //{ 
        
    //}
}
