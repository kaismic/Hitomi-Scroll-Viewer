using HitomiScrollViewerData;
using HitomiScrollViewerWebApp.Models;
using Microsoft.AspNetCore.SignalR.Client;

namespace HitomiScrollViewerWebApp.Services {
    public class DownloadClientManagerService(
        GalleryService galleryService,
        IConfiguration appConfiguration,
        DownloadConfigurationService downloadConfigurationService,
        DownloadService downloadService
        ) {
        private HubConnection? _hubConnection;
        public Dictionary<int, DownloadModel> Downloads { get; } = [];

        public bool IsHubConnectionOpen => _hubConnection?.State == HubConnectionState.Connected;
        public Func<Task>? DownloadPageStateHasChanged { get; set; }

        public void OpenHubConnection() {
            _hubConnection = new HubConnectionBuilder()
                .WithUrl(appConfiguration["ApiUrl"] + appConfiguration["DownloadHubPath"])
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
                vm.Gallery = await galleryService.GetGalleryMinDTO(galleryId);
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
            // TODO css animation fade out or something like that
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
            if (!downloadConfigurationService.Config.UseParallelDownload) {
                foreach (DownloadModel d in Downloads.Values) {
                    if (d.Status == DownloadStatus.Downloading) {
                        return;
                    }
                }
                // no currently downloading downloads so find a paused download and start it
                foreach (DownloadModel d in Downloads.Values) {
                    if (d.Status == DownloadStatus.Paused) {
                        _ = StartDownload(d.GalleryId);
                        return;
                    }
                }
            }
        }

        public void AddDownloads(IEnumerable<int> galleryIds) {
            foreach (int id in galleryIds) {
                if (Downloads.ContainsKey(id)) {
                    continue;
                }
                Downloads.Add(id, new() { GalleryId = id });
                if (downloadConfigurationService.Config.UseParallelDownload) {
                    _ = StartDownload(id);
                }
            }
            StartNext();
            DownloadPageStateHasChanged?.Invoke();
        }

        public async Task StartDownload(int id) {
            await downloadService.StartDownload(id);
        }

        public async Task PauseDownload(int id) {
            await downloadService.PauseDownload(id);
        }

        public async Task DeleteDownload(int id) {
            await downloadService.DeleteDownload(id);
            Downloads.Remove(id);
            DownloadPageStateHasChanged?.Invoke();
        }
    }
}
