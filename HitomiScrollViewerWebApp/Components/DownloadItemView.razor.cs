﻿using HitomiScrollViewerData;
using HitomiScrollViewerWebApp.Models;
using HitomiScrollViewerWebApp.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace HitomiScrollViewerWebApp.Components {
    public partial class DownloadItemView : ComponentBase, IDisposable {
        [Inject] private DownloadService DownloadService { get; set; } = null!;
        [Inject] private DownloadClientManagerService DownloadManager { get; set; } = default!;
        [Parameter, EditorRequired] public DownloadModel Model { get; set; } = null!;

        private string ControlButtonIcon => Model.Status switch {
            DownloadStatus.Downloading => Icons.Material.Filled.Pause,
            DownloadStatus.Completed => Icons.Material.Filled.Check,
            DownloadStatus.Paused or DownloadStatus.Failed => Icons.Material.Filled.PlayArrow,
            DownloadStatus.Deleted => "",
            _ => throw new NotImplementedException()
        };

        protected override void OnInitialized() {
            Model.StateHasChanged = StateHasChanged;
        }

        private async Task OnActionButtonClick() {
            Model.WaitingResponse = true;
            switch (Model.Status) {
                case DownloadStatus.Downloading:
                    await DownloadService.PauseDownloaders([Model.GalleryId]);
                    break;
                case DownloadStatus.Paused or DownloadStatus.Failed:
                    await DownloadService.StartDownloaders([Model.GalleryId]);
                    break;
                case DownloadStatus.Completed or DownloadStatus.Deleted:
                    throw new InvalidOperationException("Action button should not be clickable");
            }
        }

        private async Task OnDeleteButtonClick() {
            Model.WaitingResponse = true;
            await DownloadService.DeleteDownloaders([Model.GalleryId]);
        }

        public void Dispose() {
            GC.SuppressFinalize(this);
        }
    }
}