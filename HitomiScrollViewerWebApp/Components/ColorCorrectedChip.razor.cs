using HitomiScrollViewerWebApp.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;

namespace HitomiScrollViewerWebApp.Components {
    public partial class ColorCorrectedChip<T> : ComponentBase {
        private const string JAVASCRIPT_FILE = $"./Components/{nameof(ColorCorrectedChip<T>)}.razor.js";

        private IJSObjectReference? _jsModule;
        private ChipModel<T> _model = default!;
#pragma warning disable BL0007 // Component parameters should be auto properties
        [Parameter, EditorRequired] public ChipModel<T> Model {
            get => _model;
            set {
                if (_model == value) {
                    return;
                }
                _model = value;
                _model.SelectedChanged += InternalOnSelectedChanged;
            }
        }
#pragma warning restore BL0007 // Component parameters should be auto properties
        [Parameter, EditorRequired] public Func<ChipModel<T>, string> ToStringFunc { get; set; } = default!;
        [Parameter] public EventCallback<MudChip<ChipModel<T>>> OnClose { get; set; }
        [Parameter] public EventCallback<ChipModel<T>> OnSelectedChanged { get; set; }

        private void InternalOnSelectedChanged(ChipModel<T> model) {
            // override the default behavior of MudChip Variant display. see: https://github.com/MudBlazor/MudBlazor/issues/9731
            if (model.Selected) {
                _ = Task.Run(async () => {
                    _jsModule ??= await JsRuntime.InvokeAsync<IJSObjectReference>("import", JAVASCRIPT_FILE);
                    await _jsModule.InvokeVoidAsync("correctChipVariantClass", model.Id);
                });
            }
            OnSelectedChanged.InvokeAsync(model);
        }
    }
}