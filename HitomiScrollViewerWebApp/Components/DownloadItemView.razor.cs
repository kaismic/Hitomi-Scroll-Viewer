using HitomiScrollViewerData;
using HitomiScrollViewerWebApp.Models;
using HitomiScrollViewerWebApp.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using MudExtensions;

namespace HitomiScrollViewerWebApp.Components {
    public partial class DownloadItemView : ComponentBase, IDisposable {
        [Inject] private DownloadService DownloadService { get; set; } = null!;
        [Parameter, EditorRequired] public DownloadModel Model { get; set; } = null!;

        private bool _isWaitingResponse = false;

        public const int DELETE_ANIMATION_DURATION = 1; // seconds
        public const string DOWNLOAD_ITEM_ID_PREFIX = "download-item-";

        private string ControlButtonIcon => Model.Status switch {
            DownloadStatus.Downloading => Icons.Material.Filled.Pause,
            DownloadStatus.Completed => Icons.Material.Filled.Check,
            DownloadStatus.Paused or DownloadStatus.Failed => Icons.Material.Filled.PlayArrow,
            DownloadStatus.Deleted => "",
            _ => throw new NotImplementedException()
        };

        protected override void OnAfterRender(bool firstRender) {
            if (firstRender) {
                Model.StateHasChanged = StateHasChanged;
            }
        }

        private async Task OnActionButtonClick() {
            _isWaitingResponse = true;
            StateHasChanged();
            switch (Model.Status) {
                case DownloadStatus.Downloading:
                    await DownloadService.Pause(Model.GalleryId);
                    break;
                case DownloadStatus.Paused or DownloadStatus.Failed:
                    await DownloadService.Start(Model.GalleryId);
                    break;
                case DownloadStatus.Completed or DownloadStatus.Deleted:
                    throw new InvalidOperationException("Action button should not be clickable");
            }
            _isWaitingResponse = false;
        }

        private async Task OnDeleteButtonClick() {
            _isWaitingResponse = true;
            StateHasChanged();
            await DownloadService.Delete(Model.GalleryId);
            _isWaitingResponse = false;
        }

        public void Dispose() {
            GC.SuppressFinalize(this);
            Model.StateHasChanged = null;
        }
    }
}