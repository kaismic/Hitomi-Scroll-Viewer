namespace HitomiScrollViewerAPI.Services {
    public class HitomiUrlService {
        public required string HitomiServerInfoDomain { get; init; }
        public required string HitomiMainDomain { get; init; }
        public string HitomiGgjsAddress => $"https://{HitomiServerInfoDomain}/gg.js";
        public string GetHitomiGalleryInfoAddress(int galleryId) {
            return $"https://{HitomiServerInfoDomain}/galleries/{galleryId}.js";
        }
    }
}
