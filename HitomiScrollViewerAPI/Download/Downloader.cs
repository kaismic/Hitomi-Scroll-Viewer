using HitomiScrollViewerAPI.Hubs;
using HitomiScrollViewerData;
using Microsoft.AspNetCore.SignalR;

namespace HitomiScrollViewerAPI.Download {
    public class Downloader : IDisposable {
        private const int MAX_404_RETRY_LIMIT = 3;

        public required string ConnectionId { get; init; }
        public required int GalleryId { get; init; }
        public required IHubContext<DownloadHub, IDownloadClient> DownloadHubContext { get; init; }
        public required Action<Downloader> DownloadCompleted { get; init; }
        public required Func<Downloader, Task> Http404ErrorLimitReached { get; init; }
        public required HttpClient HttpClient { get; init; }

        private LiveServerInfo _liveServerInfo = null!;
        public required LiveServerInfo LiveServerInfo {
            get => _liveServerInfo;
            set {
                _liveServerInfo = value;
                LastLiveServerInfoUpdateTime = DateTime.UtcNow;
            }
        }
        public DateTime LastLiveServerInfoUpdateTime = DateTime.MinValue;

        private CancellationTokenSource? _cts;

        public async Task Start() {
            if (_cts != null && !_cts.IsCancellationRequested) {
                return;
            }
            _cts?.Dispose();
            _cts = new();
            for (int i = 0; i < 10; i++) {
                Console.WriteLine("ConnectionId = " + ConnectionId + " GalleryId = " + GalleryId + " " + i.ToString());
                await Task.Delay(1000, _cts.Token);
            }
            DownloadHubContext.Clients.Client(ConnectionId).ReceiveStatus(DownloadStatus.Completed, "Download completed");
            DownloadCompleted(this);
        }

        public void Pause() {
            _cts?.Cancel();
            DownloadHubContext.Clients.Client(ConnectionId).ReceiveStatus(DownloadStatus.Paused, "Download paused");
        }

        public void Remove() {
            _cts?.Cancel();
            DownloadHubContext.Clients.Client(ConnectionId).ReceiveStatus(DownloadStatus.Removed, "Download removed");
        }

        public void Dispose() {
            GC.SuppressFinalize(this);
            _cts?.Dispose();
        }
    }
}
