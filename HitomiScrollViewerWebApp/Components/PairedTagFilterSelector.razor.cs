using HitomiScrollViewerData.DTOs;
using HitomiScrollViewerWebApp.Models;
using Microsoft.AspNetCore.Components;

namespace HitomiScrollViewerWebApp.Components {
    public class PairedTagFilterSelector : TagFilterSelector {
        [Parameter, EditorRequired] public PairedTagFilterSelector Other { get; set; } = default!;

        public override async Task OnSelectedChanged(ChipModel<TagFilterDTO> model) {
            await base.OnSelectedChanged(model);
            ChipModel<TagFilterDTO>? otherChipModel = Other.ChipModels.Find(m => m.Value.Id == model.Value.Id);
            if (otherChipModel != null) {
                otherChipModel.Disabled = model.Selected;
                Other.StateHasChanged();
            }
        }
    }
}