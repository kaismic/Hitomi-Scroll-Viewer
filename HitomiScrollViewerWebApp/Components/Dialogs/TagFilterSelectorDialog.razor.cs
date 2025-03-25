using HitomiScrollViewerData.DTOs;
using HitomiScrollViewerWebApp.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace HitomiScrollViewerWebApp.Components.Dialogs {
    public partial class TagFilterSelectorDialog : ComponentBase {
        [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = null!;
        [Parameter, EditorRequired] public string ActionText { get; set; } = null!;
        [Parameter, EditorRequired] public List<ChipModel<TagFilterDTO>> ChipModels { get; set; } = default!;

        private TagFilterSelector _tagFilterSelector = default!;
        private bool _disableActionButton = true;

        protected override void OnAfterRender(bool firstRender) {
            if (firstRender) {
                _tagFilterSelector.SelectedChipModelsChanged += (collection) => {
                    _disableActionButton = _tagFilterSelector.SelectedChipModels.Count == 0;
                    StateHasChanged();
                };
            }
        }

        public void ExecuteAction() {
            MudDialog.Close(DialogResult.Ok(_tagFilterSelector.SelectedChipModels));
        }
    }
}