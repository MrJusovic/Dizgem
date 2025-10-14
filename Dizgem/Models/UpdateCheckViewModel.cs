namespace Dizgem.Models
{
    public class UpdateCheckViewModel
    {
        public string CurrentVersion { get; set; }
        public string LatestVersion { get; set; }
        public bool IsUpdateAvailable { get; set; }
        public string ReleaseNotes { get; set; }
        public string DownloadUrl { get; set; }
        public string ErrorMessage { get; set; }
    }
}
