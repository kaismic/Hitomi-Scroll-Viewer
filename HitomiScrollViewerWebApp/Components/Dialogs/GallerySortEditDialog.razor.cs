using HitomiScrollViewerData.DTOs;
using HitomiScrollViewerWebApp.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace HitomiScrollViewerWebApp.Components.Dialogs {
    public partial class GallerySortEditDialog : ComponentBase {
        [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = null!;
        [Inject] private BrowseConfigurationService BrowseConfigurationService { get; set; } = default!;

        private MudDropContainer<GallerySortDTO> _dropContainer = default!;
        private MudDropZone<GallerySortDTO> _dropZone = default!;
        private ICollection<GallerySortDTO> _sorts = [];

        protected override void OnInitialized() {
            _sorts = [.. BrowseConfigurationService.Config.Sorts.Select(s => new GallerySortDTO() {
                Property = s.Property,
                SortDirection = s.SortDirection,
                IsActive = s.IsActive,
                RankIndex = s.RankIndex
            })];
        }

        private async Task AddSort(GallerySortDTO sort) {
            sort.IsActive = true;
            _dropContainer.Refresh();
            await InvokeAsync(StateHasChanged);
        }

        private async Task RemoveSort(GallerySortDTO sort) {
            sort.IsActive = false;
            _dropContainer.Refresh();
            await InvokeAsync(StateHasChanged);
        }

        public void ExecuteAction() {
            GallerySortDTO[] activeSorts = _dropZone.GetItems();
            for (int i = 0; i < activeSorts.Length; i++) {
                activeSorts[i].RankIndex = i;
            }
            MudDialog.Close(DialogResult.Ok(_sorts));
        }

        private void Close() => MudDialog.Close(DialogResult.Cancel());
    }
}
