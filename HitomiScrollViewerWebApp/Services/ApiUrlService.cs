using Flurl;

namespace HitomiScrollViewerWebApp.Services {
    public class ApiUrlService(string baseUrl) {
        public string BaseUrl { get; } = baseUrl;
        public string GetImageUrl(int galleryId, int index) {
            return new Url(Url.Combine(BaseUrl, "api/images")).SetQueryParams(new { galleryId, index }).ToString();
        }
    }
}
