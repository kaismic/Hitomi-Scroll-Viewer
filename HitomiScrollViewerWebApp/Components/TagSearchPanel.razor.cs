using HitomiScrollViewerData.DTOs;
using HitomiScrollViewerWebApp.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;

namespace HitomiScrollViewerWebApp.Components {
    public partial class TagSearchPanel : ComponentBase {
        private const string JAVASCRIPT_FILE = $"./Components/{nameof(TagSearchPanel)}.razor.js";
        private IJSObjectReference? _jsModule;
        [Parameter] public string? Class { get; set; }
        [Parameter] public string? Style { get; set; }

        [Parameter, EditorRequired] public TagSearchPanelModel Model { get; set; } = default!;

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
                        if (_jsModule == null) {
                            JSRuntime.InvokeAsync<IJSObjectReference>("import", JAVASCRIPT_FILE).AsTask()
                                .ContinueWith((task) => {
                                    _jsModule = task.Result;
                                    _ = _jsModule.InvokeVoidAsync("scrollToElement", chipModel.Id);
                                });
                        } else {
                            _ = _jsModule.InvokeVoidAsync("scrollToElement", chipModel.Id);
                        }
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
