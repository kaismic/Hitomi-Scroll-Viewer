using HitomiScrollViewerData.DTOs;
using HitomiScrollViewerWebApp.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace HitomiScrollViewerWebApp.Components {
    public partial class TagFilterSelector : ComponentBase {
        [Parameter] public string? Class { get; set; }
        [Parameter] public string? Style { get; set; }
        [Parameter, EditorRequired] public List<ChipModel<TagFilterDTO>> ChipModels { get; set; } = default!;
        [Parameter] public string? HeaderText { get; set; }
        [Parameter] public Color HeaderColor { get; set; } = Color.Default;
        public IReadOnlyCollection<ChipModel<TagFilterDTO>> SelectedChipModels { get; set; } = [];
        public event Action<IReadOnlyCollection<ChipModel<TagFilterDTO>>>? SelectedChipModelsChanged;
        protected virtual void OnSelectedChanged(ChipModel<TagFilterDTO> model) {}
    }
}