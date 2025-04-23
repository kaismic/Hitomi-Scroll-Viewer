using HitomiScrollViewerData;
using HitomiScrollViewerData.DTOs;
using HitomiScrollViewerWebApp.Models;
using Microsoft.AspNetCore.Components;

namespace HitomiScrollViewerWebApp.Components {
    public partial class TagSearchPanelCollection {
        [Parameter] public string? Style { get; set; }
        [Parameter, EditorRequired] public List<ChipModel<TagDTO>>[] TagSearchPanelChipModels { get; set; } = default!;
        [Parameter] public EventCallback<AdvancedCollectionChangedEventArgs<ChipModel<TagDTO>>> ChipModelsChanged { get; set; }
    }
}
