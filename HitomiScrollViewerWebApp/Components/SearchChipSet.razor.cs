using HitomiScrollViewerWebApp.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace HitomiScrollViewerWebApp.Components {
    public partial class SearchChipSet<TValue> : ChipSetBase<TValue> {
        [Parameter, EditorRequired] public virtual SearchChipSetModel<TValue> Model { get; init; } = null!;
        protected virtual TValue? SearchValue { get; set; }

        private void HandleClosed(MudChip<ChipModel<TValue>> mudChip) {
            Model.ChipModels.Remove(mudChip.Value!);
        }
    }
}