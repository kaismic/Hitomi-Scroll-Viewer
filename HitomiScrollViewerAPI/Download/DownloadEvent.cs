namespace HitomiScrollViewerAPI.Download {
    public class DownloadEventArgs : EventArgs {
        public required int GalleryId { get; init; }
        public required string ConnectionId { get; init; }
    }
}
