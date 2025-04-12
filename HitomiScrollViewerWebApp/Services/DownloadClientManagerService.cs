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
            _hubConnection.On<int>("ReceiveGalleryCreated", OnReceiveGalleryCreated);
            _hubConnection.On<int, int>("ReceiveProgress", OnReceiveProgress);
            _hubConnection.On<int, DownloadStatus, string>("ReceiveStatus", OnReceiveStatus);
            _hubConnection.Closed += OnClosed;
            _hubConnection.StartAsync();
        }

        private void OnReceiveSavedDownloads(IEnumerable<int> galleryIds) {
            foreach (int id in galleryIds) {
                Downloads.Add(id, new() { GalleryId = id });
            }
            DownloadPageStateHasChanged?.Invoke();
        }

        private async Task OnReceiveGalleryCreated(int galleryId) {
            if (Downloads.TryGetValue(galleryId, out DownloadModel? vm)) {
                vm.Gallery = await galleryService.GetGalleryDownloadDTO(galleryId);
                vm.StateHasChanged?.Invoke();
            }
        }

        private void OnReceiveProgress(int galleryId, int progress) {
            if (Downloads.TryGetValue(galleryId, out DownloadModel? vm)) {
                vm.Progress = progress;
                vm.StateHasChanged?.Invoke();
            }
        }

        private void OnReceiveStatus(int galleryId, DownloadStatus status, string message) {
            if (Downloads.TryGetValue(galleryId, out DownloadModel? vm)) {
                vm.Status = status;
                switch (status) {
                    case DownloadStatus.Failed:
                        vm.StatusMessage = message;
                        break;
                    case DownloadStatus.Completed:
                        // TODO css animation fade out or something like that
                        _ = DeleteDownload(galleryId);
                        StartNext();
                        break;
                    default:
                        break;
                }
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
