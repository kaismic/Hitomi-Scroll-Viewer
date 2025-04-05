using HitomiScrollViewerData;
using HitomiScrollViewerData.DTOs;
using HitomiScrollViewerWebApp.Services;
using Microsoft.AspNetCore.SignalR.Client;
using System.Net.NetworkInformation;

namespace HitomiScrollViewerWebApp.ViewModels {
    public class DownloadViewModel : IAsyncDisposable {
        public required string DownloadHubUrl { get; init; }
        public required DownloadItemDTO DownloadItem { get; set; }
        public required DownloadStatus Status { get; set; }
        public string StatusMessage { get; set; } = "";

        public Action? StateHasChanged { get; set; }

        private HubConnection? _hubConnection;
        private IDisposable? _onReceiveGalleryInfoSub;

        public async Task StartDownload() {
            if (_hubConnection != null) {
                await _hubConnection.DisposeAsync();
            }
            _hubConnection = new HubConnectionBuilder()
                .WithUrl(DownloadHubUrl + $"?galleryId={DownloadItem.GalleryId}")
                .Build();
            _onReceiveGalleryInfoSub = _hubConnection.On<string, int>("ReceiveGalleryInfo", OnReceiveGalleryInfo);
            _hubConnection.On<int>("ReceiveProgress", OnReceiveProgress);
            _hubConnection.On<DownloadStatus, string>("ReceiveStatus", OnReceiveStatus);
            await _hubConnection.StartAsync();
        }


        private void OnReceiveGalleryInfo(string title, int count) {
            StatusMessage = "Downloading...";
            DownloadItem.Title = title;
            DownloadItem.TotalCount = count;
            StateHasChanged();
            _onReceiveGalleryInfoSub?.Dispose();
        }

        private void OnReceiveProgress(int progress) {
            DownloadItem.Progress = progress;
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
            StateHasChanged();
        }


        public async ValueTask DisposeAsync() {
            if (_hubConnection != null) {
                await _hubConnection.DisposeAsync();
            }
        }
    }
}
