using HitomiScrollViewerWebApp.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace HitomiScrollViewerWebApp.Components {
    public partial class SearchChipSet<TValue> : ComponentBase {
        [Parameter, EditorRequired] public virtual SearchChipSetModel<TValue> Model { get; init; } = null!;

        private void HandleClosed(MudChip<TValue> mudChip) {
            Model.Values.Remove(mudChip.Value!);
        }
    }
}