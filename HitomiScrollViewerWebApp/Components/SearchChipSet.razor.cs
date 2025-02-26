using HitomiScrollViewerWebApp.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;

namespace HitomiScrollViewerWebApp.Components {
    public partial class SearchChipSet<TValue> : ComponentBase {
        [Parameter, EditorRequired] public virtual SearchChipSetModel<TValue> Model { get; init; } = null!;
        [Inject] protected IJSRuntime JSRuntime { get; set; } = null!;
        protected virtual TValue? SearchValue { get; set; }

        private async Task HandleChipClick(SearchChipModel<TValue> model) {
            Console.WriteLine(model.Id);
            // override the default behavior of MudChip Variant display. see: https://github.com/MudBlazor/MudBlazor/issues/9731
            await JSRuntime.InvokeVoidAsync("setChipClass", model);
        }

        private void HandleClosed(MudChip<SearchChipModel<TValue>> mudChip) {
            Model.ChipModels.Remove(mudChip.Value!);
        }
    }
}