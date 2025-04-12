using HitomiScrollViewerData;

namespace HitomiScrollViewerAPI.Hubs {
    public interface IDownloadClient {
        Task ReceiveSavedDownloads(IEnumerable<int> galleryIds);
        Task ReceiveGalleryCreated(int galleryId);
        Task ReceiveProgress(int galleryId, int progress);
        Task ReceiveStatus(int galleryId, DownloadStatus status, string message);
    }
}
