using HitomiScrollViewerData;
using HitomiScrollViewerData.DTOs;
using HitomiScrollViewerWebApp.Services;
using Microsoft.AspNetCore.SignalR.Client;

namespace HitomiScrollViewerWebApp.ViewModels {
    public class DownloadViewModel : IAsyncDisposable {
        public required GalleryService GalleryService { get; init; }
        public required PageConfigurationService PageConfigurationService { get; init; }
        public required string DownloadHubUrl { get; init; }
        public required int GalleryId { get; init; }
        public DownloadStatus Status { get; set; } = DownloadStatus.Pending;
        public GalleryDownloadDTO? Gallery { get; set; }
        public string StatusMessage { get; set; } = "";
        public int Progress { get; set; }
        public Action? StateHasChanged { get; set; }

        private HubConnection? _hubConnection;

        public async Task StartDownload() {
            Gallery ??= await GalleryService.GetGalleryDownloadDTO(GalleryId);
            if (_hubConnection == null || _hubConnection.State == HubConnectionState.Disconnected) {
                _hubConnection = new HubConnectionBuilder()
                    .WithUrl(DownloadHubUrl + $"?GalleryId={GalleryId}&ConfigId={PageConfigurationService.DownloadConfiguration.Id}")
                    .Build();
                _hubConnection.On("ReceiveGalleryCreated", OnReceiveGalleryCreated);
                _hubConnection.On<int>("ReceiveProgress", OnReceiveProgress);
                _hubConnection.On<DownloadStatus, string>("ReceiveStatus", OnReceiveStatus);
                _hubConnection.Closed += OnClosed;
                await _hubConnection.StartAsync();
            } else {
                await _hubConnection.SendAsync("Resume");
            }
        }

        private async Task SendPause() {
            if (_hubConnection != null) {
                await _hubConnection.SendAsync("Pause");
            }
        }

        public async Task Pause() {
            await SendPause();
            Status = DownloadStatus.Paused;
            StatusMessage = "Download paused";
            StateHasChanged?.Invoke();
        }

        public async Task SendDisconnect() {
            if (_hubConnection != null) {
                await _hubConnection.SendAsync("Disconnect");
                await _hubConnection.DisposeAsync();
            }
        }

        private async Task OnReceiveGalleryCreated() {
            Gallery = await GalleryService.GetGalleryDownloadDTO(GalleryId);
            StateHasChanged?.Invoke();
        }

        private void OnReceiveProgress(int progress) {
            Progress = progress;
            StateHasChanged?.Invoke();
        }

        private void OnReceiveStatus(DownloadStatus status, string message) {
            Status = status;
            switch (status) {
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

        private async Task OnClosed(Exception? e) {
            if (_hubConnection != null) {
                await _hubConnection.DisposeAsync();
                _hubConnection = null;
            }
        }

        public async ValueTask DisposeAsync() {
            GC.SuppressFinalize(this);
            if (_hubConnection != null) {
                await _hubConnection.DisposeAsync();
            }
        }
    }
}
