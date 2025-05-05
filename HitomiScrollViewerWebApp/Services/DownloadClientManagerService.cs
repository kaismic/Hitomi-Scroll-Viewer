using HitomiScrollViewerData;
using HitomiScrollViewerWebApp.Models;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;

namespace HitomiScrollViewerWebApp.Services {
    public class DownloadClientManagerService : IAsyncDisposable {
        private readonly GalleryService _galleryService;
        private readonly IConfiguration _hostConfiguration;
        private readonly DownloadConfigurationService _downloadConfigurationService;
        private readonly DownloadService _downloadService;
        private readonly IJSRuntime _jsRuntime;

        public const string DOWNLOAD_ITEM_ID_PREFIX = "download-item-";
        private const int DELETE_ANIMATION_DURATION = 1000; // ms
        private HubConnection? _hubConnection;
        public Dictionary<int, DownloadModel> Downloads { get; } = [];

        public bool IsHubConnectionOpen => _hubConnection?.State == HubConnectionState.Connected;
        public Func<Task>? DownloadPageStateHasChanged { get; set; }
        private readonly DotNetObjectReference<DownloadClientManagerService> _dotNetObjectRef;

        public DownloadClientManagerService(
            GalleryService galleryService,
            IConfiguration hostConfiguration,
            DownloadConfigurationService downloadConfigurationService,
            DownloadService downloadService,
            IJSRuntime jsRuntime
        ) {
            _galleryService = galleryService;
            _hostConfiguration = hostConfiguration;
            _downloadConfigurationService = downloadConfigurationService;
            _downloadService = downloadService;
            _jsRuntime = jsRuntime;
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
            _hubConnection.On<int>("ReceiveComplete", OnReceiveComplete);
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
            if (Downloads.TryGetValue(galleryId, out DownloadModel? vm)) {
                vm.Gallery = await _galleryService.GetDownloadGalleryDTO(galleryId);
                vm.StateHasChanged?.Invoke();
            }
        }

        private void OnReceiveProgress(int galleryId, int progress) {
            if (Downloads.TryGetValue(galleryId, out DownloadModel? vm)) {
                vm.Progress = progress;
                vm.StateHasChanged?.Invoke();
            }
        }

        private void OnReceiveStatus(int galleryId, DownloadStatus status) {
            if (status is DownloadStatus.Completed or DownloadStatus.Failed) {
                throw new ArgumentException(
                    $"Status {nameof(DownloadStatus.Completed)} and {nameof(DownloadStatus.Failed)} must be handled by ReceiveComplete and ReceiveFailure",
                    nameof(status)
                );
            }
            if (Downloads.TryGetValue(galleryId, out DownloadModel? vm)) {
                vm.Status = status;
                vm.StateHasChanged?.Invoke();
            }
        }

        private void OnReceiveComplete(int galleryId) {
            if (Downloads.TryGetValue(galleryId, out DownloadModel? vm)) {
                vm.Status = DownloadStatus.Completed;
                vm.StatusMessage = "Download complete";
                vm.StateHasChanged?.Invoke();
            }
            _ = DeleteDownload(galleryId);
            StartNext();
        }

        private void OnReceiveFailure(int galleryId, string message) {
            if (Downloads.TryGetValue(galleryId, out DownloadModel? vm)) {
                vm.Status = DownloadStatus.Failed;
                vm.StatusMessage = message;
                vm.StateHasChanged?.Invoke();
            }
        }

        private async Task OnClosed(Exception? e) {
            if (_hubConnection != null) {
                await _hubConnection.DisposeAsync();
                _hubConnection = null;
            }
        }

        private void StartNext() {
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
                    _ = StartDownload(firstPaused.GalleryId);
                }
            }
        }

        public void AddDownloads(IEnumerable<int> galleryIds) {
            foreach (int id in galleryIds) {
                if (Downloads.ContainsKey(id)) {
                    continue;
                }
                Downloads.Add(id, new() { GalleryId = id });
                if (_downloadConfigurationService.Config.UseParallelDownload) {
                    _ = StartDownload(id);
                }
            }
            StartNext();
            DownloadPageStateHasChanged?.Invoke();
        }

        public async Task StartDownload(int id) {
            await _downloadService.StartDownload(id);
        }

        public async Task PauseDownload(int id) {
            await _downloadService.PauseDownload(id);
        }

        public async Task DeleteDownload(int id) {
            await _jsRuntime.InvokeVoidAsync("startDeleteAnimation", DOWNLOAD_ITEM_ID_PREFIX + id, id, DELETE_ANIMATION_DURATION, _dotNetObjectRef);
            await _downloadService.DeleteDownload(id);
        }

        [JSInvokable]
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
