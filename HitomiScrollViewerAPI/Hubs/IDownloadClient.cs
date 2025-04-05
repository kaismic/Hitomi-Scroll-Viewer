using HitomiScrollViewerData;

namespace HitomiScrollViewerAPI.Hubs {
    public interface IDownloadClient {
        void ReceiveGalleryInfo(string title, int count);
        void ReceiveProgress(int progress);
        void ReceiveStatus(DownloadStatus status, string message);
    }
}
