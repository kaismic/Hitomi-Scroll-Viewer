using HitomiScrollViewerData.DTOs;
using HitomiScrollViewerWebApp.Models;
using Microsoft.AspNetCore.Components;

namespace HitomiScrollViewerWebApp.Components {
    public class PairedTagFilterSelector : TagFilterSelector {
        [Parameter, EditorRequired] public PairedTagFilterSelector Other { get; set; } = default!;

        protected override void OnSelectedChanged(ChipModel<TagFilterDTO> model) {
            base.OnSelectedChanged(model);
            ChipModel<TagFilterDTO>? otherModel = Other.ChipModels.Find(m => m.Value.Id == model.Value.Id);
            if (otherModel != null) {
                otherModel.Disabled = model.Selected;
                Other.StateHasChanged();
            }
        }
    }
}