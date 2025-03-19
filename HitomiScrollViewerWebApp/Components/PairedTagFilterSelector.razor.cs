using HitomiScrollViewerData.DTOs;
using HitomiScrollViewerWebApp.Models;

namespace HitomiScrollViewerWebApp.Components {
    public class PairedTagFilterSelector : TagFilterSelector {
        public required PairedTagFilterSelector Other { private get; set; }
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