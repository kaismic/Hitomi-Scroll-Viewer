using HitomiScrollViewerData.DTOs;
using HitomiScrollViewerWebApp.Models;
using Microsoft.AspNetCore.Components;

namespace HitomiScrollViewerWebApp.Components {
    public partial class TagSearchPanelCollection {
        [Parameter, EditorRequired] public List<ChipModel<TagDTO>>[] TagSearchPanelChipModels { get; set; } = default!;
    }
}
