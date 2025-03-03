using HitomiScrollViewerData.DTOs;
using HitomiScrollViewerWebApp.Models;
using Microsoft.AspNetCore.Components;

namespace HitomiScrollViewerWebApp.Components {
    public partial class TagFilterSelector : ChipSetBase<TagFilterDTO> {
        [Parameter, EditorRequired] public List<ChipModel<TagFilterDTO>> ChipModels { get; set; } = [];
        public IReadOnlyCollection<ChipModel<TagFilterDTO>> SelectedChipModels { get; } = [];
    }
}