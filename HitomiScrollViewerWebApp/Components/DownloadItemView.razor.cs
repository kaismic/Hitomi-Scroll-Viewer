using HitomiScrollViewerData;
using HitomiScrollViewerWebApp.Services;
using HitomiScrollViewerWebApp.ViewModels;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace HitomiScrollViewerWebApp.Components {
    public partial class DownloadItemView : ComponentBase, IDisposable {
        [Inject] private DownloadManagerService DownloadManagerService { get; set; } = null!;
        [Parameter, EditorRequired] public DownloadViewModel ViewModel { get; set; } = null!;

        private string ControlButtonIcon => ViewModel.Status switch {
            DownloadStatus.Pending => Icons.Material.Filled.Pending,
            DownloadStatus.Downloading => Icons.Material.Filled.Pause,
            DownloadStatus.Completed => Icons.Material.Filled.Check,
            DownloadStatus.Paused or DownloadStatus.Failed => Icons.Material.Filled.PlayArrow,
            _ => throw new NotImplementedException()
        };

        protected override void OnAfterRender(bool firstRender) {
            if (firstRender) {
                ViewModel.StateHasChanged = StateHasChanged;
            }
        }

        private void OnActionButtonClick() {
            switch (ViewModel.Status) {
                case DownloadStatus.Pending:
                    throw new InvalidOperationException("Action button should not be clickable");
                case DownloadStatus.Downloading:
                    DownloadManagerService.PauseDownload(ViewModel.GalleryId);
                    break;
                case DownloadStatus.Completed:
                    throw new InvalidOperationException("Action button should not be clickable");
                case DownloadStatus.Paused or DownloadStatus.Failed:
                    DownloadManagerService.StartDownload(ViewModel.GalleryId);
                    break;
            }
        }

        private void OnDeleteButtonClick() {
            DownloadManagerService.RemoveDownload(ViewModel.GalleryId);
        }

        public void Dispose() {
            GC.SuppressFinalize(this);
            ViewModel.StateHasChanged = null;
        }
    }
}