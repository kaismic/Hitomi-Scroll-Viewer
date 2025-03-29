using HitomiScrollViewerData.DTOs;
using HitomiScrollViewerWebApp.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace HitomiScrollViewerWebApp.Components.Dialogs {
    public partial class TagFilterSelectorDialog : ComponentBase {
        [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = null!;
        [Parameter, EditorRequired] public string ActionText { get; set; } = null!;
        [Parameter, EditorRequired] public List<ChipModel<TagFilterDTO>> ChipModels { get; set; } = default!;
        private IReadOnlyCollection<ChipModel<TagFilterDTO>> SelectedChipModels { get; set; } = [];

        public void ExecuteAction() {
            MudDialog.Close(DialogResult.Ok(SelectedChipModels));
        }
    }
}