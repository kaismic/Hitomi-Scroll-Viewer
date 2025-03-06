using HitomiScrollViewerData.DTOs;
using HitomiScrollViewerWebApp.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace HitomiScrollViewerWebApp.Components {
    public partial class TagFilterSelector : ChipSetBase<TagFilterDTO> {
        [Parameter, EditorRequired] public List<ChipModel<TagFilterDTO>> ChipModels { get; set; } = [];
        [Parameter, EditorRequired] public string? HeaderText { get; set; }
        [Parameter] public Color HeaderTextColor { get; set; } = Color.Default;
        public IReadOnlyCollection<ChipModel<TagFilterDTO>> SelectedChipModels { get; set; } = [];
        public event Action<IReadOnlyCollection<ChipModel<TagFilterDTO>>?>? SelectedChipModelsChanged;
    }
}