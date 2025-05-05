using HitomiScrollViewerData;
using HitomiScrollViewerWebApp.Components;
using HitomiScrollViewerWebApp.Models;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;

namespace HitomiScrollViewerWebApp.Services {
    public class DownloadClientManagerService : IAsyncDisposable {
        private readonly GalleryService _galleryService;
        private readonly IConfiguration _hostConfiguration;
        private readonly DownloadConfigurationService _downloadConfigurationService;
        private readonly DownloadService _downloadService;

        private HubConnection? _hubConnection;
        public Dictionary<int, DownloadModel> Downloads { get; } = [];

        public bool IsHubConnectionOpen => _hubConnection?.State == HubConnectionState.Connected;
        public Func<Task>? DownloadPageStateHasChanged { get; set; }
        private readonly DotNetObjectReference<DownloadClientManagerService> _dotNetObjectRef;

        public DownloadClientManagerService(
            GalleryService galleryService,
            IConfiguration hostConfiguration,
            DownloadConfigurationService downloadConfigurationService,
            DownloadService downloadService
        ) {
            _galleryService = galleryService;
            _hostConfiguration = hostConfiguration;
            _downloadConfigurationService = downloadConfigurationService;
            _downloadService = downloadService;
            _dotNetObjectRef = DotNetObjectReference.Create(this);
        }

        public void OpenHubConnection() {
            _hubConnection = new HubConnectionBuilder()
                .WithUrl(_hostConfiguration["ApiUrl"] + _hostConfiguration["DownloadHubPath"])
                .Build();
            _hubConnection.On<IEnumerable<int>>("ReceiveSavedDownloads", OnReceiveSavedDownloads);
            _hubConnection.On<int>("ReceiveGalleryAvailable", OnReceiveGalleryAvailable);
            _hubConnection.On<int, int>("ReceiveProgress", OnReceiveProgress);
            _hubConnection.On<int, DownloadStatus>("ReceiveStatus", OnReceiveStatus);
            _hubConnection.On<int, string>("ReceiveFailure", OnReceiveFailure);
            _hubConnection.Closed += OnClosed;
            _hubConnection.StartAsync();
        }

        private void OnReceiveSavedDownloads(IEnumerable<int> galleryIds) {
            foreach (int id in galleryIds) {
                Downloads.Add(id, new() { GalleryId = id });
            }
            DownloadPageStateHasChanged?.Invoke();
        }

        private async Task OnReceiveGalleryAvailable(int galleryId) {
            if (Downloads.TryGetValue(galleryId, out DownloadModel? model)) {
                model.Gallery = await _galleryService.GetDownloadGalleryDTO(galleryId);
                model.StateHasChanged?.Invoke();
            }
        }

        private void OnReceiveProgress(int galleryId, int progress) {
            if (Downloads.TryGetValue(galleryId, out DownloadModel? model)) {
                model.Progress = progress;
                model.StateHasChanged?.Invoke();
            }
        }

        private void OnReceiveStatus(int galleryId, DownloadStatus status) {
            if (Downloads.TryGetValue(galleryId, out DownloadModel? model)) {
                model.Status = status;
                switch (status) {
                    case DownloadStatus.Downloading:
                        model.StatusMessage = "Downloading...";
                        break;
                    case DownloadStatus.Completed:
                        model.StatusMessage = "Download completed.";
                        // TODO
                        //model.DeleteAnimationPaused = false;
                        //_ = Task.Delay(DownloadItemView.DELETE_ANIMATION_DURATION * 1000).ContinueWith(_ => OnDeleteAnimationFinished(model.GalleryId));
                        //_ = _jsRuntime.InvokeVoidAsync("startDeleteAnimation", DOWNLOAD_ITEM_ID_PREFIX + galleryId, galleryId, DELETE_ANIMATION_DURATION, _dotNetObjectRef);
                        break;
                    case DownloadStatus.Paused:
                        model.StatusMessage = "Download paused.";
                        break;
                    case DownloadStatus.Deleted:
                        model.StatusMessage = "";
                        // TODO
                        //model.DeleteAnimationPaused = false;
                        //_ = Task.Delay(DownloadItemView.DELETE_ANIMATION_DURATION * 1000).ContinueWith(_ => OnDeleteAnimationFinished(model.GalleryId));
                        //_ = _jsRuntime.InvokeVoidAsync("startDeleteAnimation", DOWNLOAD_ITEM_ID_PREFIX + galleryId, galleryId, DELETE_ANIMATION_DURATION, _dotNetObjectRef);
                        break;
                    case DownloadStatus.Failed:
                        throw new InvalidOperationException($"{DownloadStatus.Failed} must be handled by {nameof(OnReceiveFailure)}");
                }
                model.StateHasChanged?.Invoke();
            }
        }

        private void OnReceiveFailure(int galleryId, string message) {
            if (Downloads.TryGetValue(galleryId, out DownloadModel? model)) {
                model.Status = DownloadStatus.Failed;
                model.StatusMessage = message;
                model.StateHasChanged?.Invoke();
            }
        }

        private async Task OnClosed(Exception? e) {
            if (_hubConnection != null) {
                await _hubConnection.DisposeAsync();
                _hubConnection = null;
            }
        }

        public void AddDownloads(IEnumerable<int> galleryIds) {
            foreach (int id in galleryIds) {
                if (Downloads.ContainsKey(id)) {
                    continue;
                }
                Downloads.Add(id, new() { GalleryId = id });
                if (_downloadConfigurationService.Config.UseParallelDownload) {
                    _ = _downloadService.Start(id);
                } else {
                    _ = _downloadService.Create(id);
                }
            }
            if (!_downloadConfigurationService.Config.UseParallelDownload) {
                DownloadModel? firstPaused = null;
                foreach (DownloadModel d in Downloads.Values) {
                    if (d.Status == DownloadStatus.Downloading) {
                        return;
                    } else if (firstPaused == null && d.Status == DownloadStatus.Paused) {
                        firstPaused = d;
                    }
                }
                // no currently downloading downloads so start the first paused download
                if (firstPaused != null) {
                    _ = _downloadService.Start(firstPaused.GalleryId);
                }
            }
            DownloadPageStateHasChanged?.Invoke();
        }

        public void OnDeleteAnimationFinished(int galleryId) {
            Downloads.Remove(galleryId);
            DownloadPageStateHasChanged?.Invoke();
        }

        public async ValueTask DisposeAsync() {
            GC.SuppressFinalize(this);
            if (_hubConnection != null) {
                await _hubConnection.DisposeAsync();
                _hubConnection = null;
            }
            _dotNetObjectRef.Dispose();
        }
    }
}
