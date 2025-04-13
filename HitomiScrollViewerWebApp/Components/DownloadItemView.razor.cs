using HitomiScrollViewerData;
using HitomiScrollViewerWebApp.Models;
using HitomiScrollViewerWebApp.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace HitomiScrollViewerWebApp.Components {
    public partial class DownloadItemView : ComponentBase, IDisposable {
        [Inject] private DownloadClientManagerService DownloadManagerService { get; set; } = null!;
        [Parameter, EditorRequired] public DownloadModel ViewModel { get; set; } = null!;

        private string ControlButtonIcon => ViewModel.Status switch {
            DownloadStatus.Downloading or DownloadStatus.WaitingLSIUpdate => Icons.Material.Filled.Pause,
            DownloadStatus.Completed => Icons.Material.Filled.Check,
            DownloadStatus.Paused or DownloadStatus.Failed => Icons.Material.Filled.PlayArrow,
            _ => throw new NotImplementedException()
        };

        protected override void OnAfterRender(bool firstRender) {
            if (firstRender) {
                ViewModel.StateHasChanged = StateHasChanged;
            }
        }

        private async Task OnActionButtonClick() {
            switch (ViewModel.Status) {
                case DownloadStatus.Downloading or DownloadStatus.WaitingLSIUpdate:
                    await DownloadManagerService.PauseDownload(ViewModel.GalleryId);
                    break;
                case DownloadStatus.Completed:
                    throw new InvalidOperationException("Action button should not be clickable");
                case DownloadStatus.Paused or DownloadStatus.Failed:
                    await DownloadManagerService.StartDownload(ViewModel.GalleryId);
                    break;
            }
        }

        private async Task OnDeleteButtonClick() {
            await DownloadManagerService.DeleteDownload(ViewModel.GalleryId);
        }

        public void Dispose() {
            GC.SuppressFinalize(this);
            ViewModel.StateHasChanged = null;
        }
    }
}