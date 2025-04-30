using HitomiScrollViewerData;
using HitomiScrollViewerWebApp.Models;
using HitomiScrollViewerWebApp.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace HitomiScrollViewerWebApp.Components {
    public partial class DownloadItemView : ComponentBase, IDisposable {
        [Parameter(CaptureUnmatchedValues = true)]
        public Dictionary<string, object?> UserAttributes { get; set; } = [];
        [Inject] private DownloadClientManagerService DownloadManagerService { get; set; } = null!;
        [Parameter, EditorRequired] public DownloadModel Model { get; set; } = null!;

        private string ControlButtonIcon => Model.Status switch {
            DownloadStatus.Downloading or DownloadStatus.WaitingLSIUpdate => Icons.Material.Filled.Pause,
            DownloadStatus.Completed => Icons.Material.Filled.Check,
            DownloadStatus.Paused or DownloadStatus.Failed => Icons.Material.Filled.PlayArrow,
            _ => throw new NotImplementedException()
        };

        protected override void OnAfterRender(bool firstRender) {
            if (firstRender) {
                Model.StateHasChanged = StateHasChanged;
            }
        }

        private async Task OnActionButtonClick() {
            switch (Model.Status) {
                case DownloadStatus.Downloading or DownloadStatus.WaitingLSIUpdate:
                    await DownloadManagerService.PauseDownload(Model.GalleryId);
                    break;
                case DownloadStatus.Completed:
                    throw new InvalidOperationException("Action button should not be clickable");
                case DownloadStatus.Paused or DownloadStatus.Failed:
                    await DownloadManagerService.StartDownload(Model.GalleryId);
                    break;
            }
        }

        private async Task OnDeleteButtonClick() {
            await DownloadManagerService.DeleteDownload(Model.GalleryId);
        }

        public void Dispose() {
            GC.SuppressFinalize(this);
            Model.StateHasChanged = null;
        }
    }
}