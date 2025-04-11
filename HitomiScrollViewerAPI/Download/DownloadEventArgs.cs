using HitomiScrollViewerData;

namespace HitomiScrollViewerAPI.Download {
    public class DownloadEventArgs : EventArgs {
        public required DownloadHubRequest DownloadRequest { get; init; }
        public required string ConnectionId { get; init; }
        public int GalleryId { get; init; }
    }
}
