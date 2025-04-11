using HitomiScrollViewerData;

namespace HitomiScrollViewerAPI.Hubs {
    public interface IDownloadClient {
        Task ReceiveGalleryCreated();
        Task ReceiveProgress(int progress);
        Task ReceiveStatus(DownloadStatus status, string message);
    }
}
