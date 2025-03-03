using HitomiScrollViewerWebApp.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace HitomiScrollViewerWebApp.Components {
    public abstract class ChipSetBase<TValue> : ComponentBase {
        [Inject] protected IJSRuntime JSRuntime { get; set; } = null!;

        protected async Task HandleChipClick(ChipModel<TValue> model) {
            // override the default behavior of MudChip Variant display. see: https://github.com/MudBlazor/MudBlazor/issues/9731
            await JSRuntime.InvokeVoidAsync("setChipClass", model);
        }
    }
}
