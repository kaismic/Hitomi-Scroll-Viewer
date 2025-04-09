using HitomiScrollViewerData;
using HitomiScrollViewerData.DTOs;

namespace HitomiScrollViewerAPI.Download {
    public class DownloadEventArgs : EventArgs {
        public required DownloadRequest DownloadRequest { get; init; }
        public required string ConnectionId { get; init; }
        public DownloadItemDTO? DownloadItem { get; init; }
    }
}
