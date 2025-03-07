using HitomiScrollViewerData.DTOs;
using HitomiScrollViewerWebApp.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;

namespace HitomiScrollViewerWebApp.Components {
    public partial class TagSearchChipSet : ChipSetBase<TagDTO> {
        [Parameter, EditorRequired] public virtual TagSearchChipSetModel Model { get; init; } = null!;

        private TagDTO? _searchValue;
        public TagDTO? SearchValue {
            get => _searchValue;
            set {
                _searchValue = value;
                if (value != null) {
                    ChipModel<TagDTO>? chipModel = Model.ChipModels.Find(m => m.Value.Id == value.Id);
                    if (chipModel == null) {
                        // create new ChipModel
                        Model.ChipModels.Add(new ChipModel<TagDTO> { Value = value });
                        _searchValue = null;
                    } else {
                        // already exists in ChipModels
#pragma warning disable CA2012 // Use ValueTasks correctly
                        _ = JSRuntime.InvokeVoidAsync("scrollToElement", chipModel.Id);
#pragma warning restore CA2012 // Use ValueTasks correctly
                    }
                }
            }
        }

        private void HandleClosed(MudChip<ChipModel<TagDTO>> mudChip) {
            Model.ChipModels.Remove(mudChip.Value!);
        }
    }
}
