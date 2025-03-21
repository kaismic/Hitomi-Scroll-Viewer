using HitomiScrollViewerData.DTOs;
using HitomiScrollViewerWebApp.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace HitomiScrollViewerWebApp.Components {
    public partial class TagFilterSelector : ChipSetBase<TagFilterDTO> {
        [Parameter] public string? Class { get; set; }
        [Parameter] public string? Style { get; set; }

        private List<ChipModel<TagFilterDTO>> _chipModels = default!;
#pragma warning disable BL0007 // Component parameters should be auto properties
        [Parameter, EditorRequired] public List<ChipModel<TagFilterDTO>> ChipModels {
            get => _chipModels;
            set {
                if (_chipModels != value) {
                    _chipModels = value;
                    for (int i = 0; i < _chipModels.Count; i++) {
                        int j = i;
                        _chipModels[j].SelectedChanged += OnSelectedChanged;
                    }
                }
            }
        }
#pragma warning restore BL0007 // Component parameters should be auto properties
        [Parameter] public string? HeaderText { get; set; }
        [Parameter] public Color HeaderTextColor { get; set; } = Color.Default;
        public event Action<IReadOnlyCollection<ChipModel<TagFilterDTO>>>? SelectedChipModelsChanged;
        public IReadOnlyCollection<ChipModel<TagFilterDTO>> SelectedChipModels { get; set; } = null!;
    }
}