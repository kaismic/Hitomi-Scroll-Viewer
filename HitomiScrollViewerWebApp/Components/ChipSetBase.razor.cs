using HitomiScrollViewerWebApp.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Reflection;

namespace HitomiScrollViewerWebApp.Components {
    public abstract partial class ChipSetBase<TValue> : ComponentBase {
        private const string JAVASCRIPT_FILE = $"./Components/{nameof(ChipSetBase)}.razor.js";

        [Parameter] public RenderFragment? ChildContent { get; set; }
        [Inject] protected IJSRuntime JsRuntime { get; set; } = null!;
        private IJSObjectReference? _jsModule;

        protected virtual async Task HandleChipClick(ChipModel<TValue> model) {
            // override the default behavior of MudChip Variant display. see: https://github.com/MudBlazor/MudBlazor/issues/9731
            _jsModule ??= await JsRuntime.InvokeAsync<IJSObjectReference>("import", JAVASCRIPT_FILE);
            await _jsModule.InvokeVoidAsync("setChipClass", model.Id);
        }
    }
}
