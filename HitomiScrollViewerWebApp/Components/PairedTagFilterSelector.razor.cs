using HitomiScrollViewerData.DTOs;
using HitomiScrollViewerWebApp.Models;

namespace HitomiScrollViewerWebApp.Components {
    public class PairedTagFilterSelector : TagFilterSelector {
        public required PairedTagFilterSelector Other { private get; set; }
        protected override Task HandleChipClick(ChipModel<TagFilterDTO> model) {
            ChipModel<TagFilterDTO>? otherChipModel = Other.ChipModels.Find(m => m.Value.Id == model.Value.Id);
            if (otherChipModel != null) {
                otherChipModel.Disabled = !otherChipModel.Disabled;
                Other.StateHasChanged();
            }
            return base.HandleChipClick(model);
        }
    }
}
