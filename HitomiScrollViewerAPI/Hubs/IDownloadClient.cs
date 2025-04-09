using HitomiScrollViewerData;

namespace HitomiScrollViewerAPI.Hubs {
    public interface IDownloadClient {
        void ReceiveGalleryCreated();
        void ReceiveProgress(int progress);
        void ReceiveStatus(DownloadStatus status, string message);
    }
}
