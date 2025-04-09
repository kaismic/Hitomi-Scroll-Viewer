using HitomiScrollViewerData;
using HitomiScrollViewerData.DTOs;
using HitomiScrollViewerWebApp.Services;
using Microsoft.AspNetCore.SignalR.Client;

namespace HitomiScrollViewerWebApp.ViewModels {
    public class DownloadViewModel : IAsyncDisposable {
        public required GalleryService GalleryService { get; set; }
        public required string DownloadHubUrl { get; init; }
        public required DownloadItemDTO DownloadItem { get; init; }
        public required HttpClient HttpClient { get; init; }
        public required DownloadStatus Status { get; set; }
        public GalleryDownloadDTO? Gallery { get; set; }
        public string StatusMessage { get; set; } = "";
        public int Progress { get; set; }

        public Action? StateHasChanged { get; set; }

        private HubConnection? _hubConnection;
        private IDisposable? _onReceiveGalleryInfoSub;

        public async Task StartDownload() {
            Gallery ??= await GalleryService.GetGalleryDownloadDTO(DownloadItem.GalleryId);
            if (_hubConnection == null) {
                _hubConnection = new HubConnectionBuilder()
                    .WithUrl(DownloadHubUrl + $"?GalleryId={DownloadItem.GalleryId}&ConfigId={DownloadItem.DownloadConfigurationId}")
                    .Build();
                _onReceiveGalleryInfoSub = _hubConnection.On("ReceiveGalleryCreated", OnReceiveGalleryCreated);
                _hubConnection.On<int>("ReceiveProgress", OnReceiveProgress);
                _hubConnection.On<DownloadStatus, string>("ReceiveStatus", OnReceiveStatus);
                await _hubConnection.StartAsync();
            } else {
                await _hubConnection.SendAsync("Resume");
            }
        }

        public async Task Pause() {
            if (_hubConnection != null) {
                await _hubConnection.SendAsync("Pause");
            }
        }

        public async Task Remove() {
            if (_hubConnection != null) {
                await _hubConnection.SendAsync("Remove");
                await _hubConnection.DisposeAsync();
            }
        }

        private async Task OnReceiveGalleryCreated() {
            Gallery = await GalleryService.GetGalleryDownloadDTO(DownloadItem.GalleryId);
            StateHasChanged?.Invoke();
            _onReceiveGalleryInfoSub?.Dispose();
        }

        private void OnReceiveProgress(int progress) {
            Progress = progress;
            StateHasChanged?.Invoke();
        }

        private void OnReceiveStatus(DownloadStatus status, string message) {
            switch (status) {
                case DownloadStatus.Paused:
                    StatusMessage = "Download paused";
                    break;
                case DownloadStatus.Failed:
                    StatusMessage = message;
                    break;
                case DownloadStatus.Completed:
                    StatusMessage = "Download completed";
                    break;
                default:
                    break;
            }
            StateHasChanged?.Invoke();
        }

        public async ValueTask DisposeAsync() {
            GC.SuppressFinalize(this);
            if (_hubConnection != null) {
                await _hubConnection.DisposeAsync();
            }
        }
    }
}
