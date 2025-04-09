using HitomiScrollViewerData;
using HitomiScrollViewerWebApp.ViewModels;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace HitomiScrollViewerWebApp.Components {
    public partial class DownloadItemView : ComponentBase {
        [Parameter, EditorRequired] public DownloadViewModel ViewModel { get; set; } = null!;

        private string ControlButtonIcon => ViewModel.Status switch {
            DownloadStatus.Paused => Icons.Material.Filled.PlayArrow,
            DownloadStatus.Downloading => Icons.Material.Filled.Pause,
            DownloadStatus.Completed => Icons.Material.Filled.Pause,
            _ => Icons.Material.Filled.PlayArrow
        };

        protected override void OnAfterRender(bool firstRender) {
            if (firstRender) {
                ViewModel.StateHasChanged = StateHasChanged;
            }
        }
    }
}