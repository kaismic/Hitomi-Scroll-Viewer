using Flurl;

namespace HitomiScrollViewerWebApp.Services {
    public class ApiUrlService(string baseUrl) {
        public string DbInitializeHubUrl { get; } = baseUrl + "api/initialize";
        public string DownloadHubUrl { get; } = baseUrl + "api/download";

        public string GetImageUrl(int galleryId, int index) {
            return new Url(Url.Combine(baseUrl, "api/images")).SetQueryParams(new { galleryId, index }).ToString();
        }
    }
}
