using HitomiScrollViewerData.DTOs;
using HitomiScrollViewerData.Entities;
using HitomiScrollViewerWebApp.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;

namespace HitomiScrollViewerWebApp.Components {
    public partial class TagSearchPanel : ComponentBase {
        private const string JAVASCRIPT_FILE = $"./Components/{nameof(TagSearchPanel)}.razor.js";
        private IJSObjectReference? _jsModule;

        [Parameter, EditorRequired] public int GridColumn { get; set; }
        [Parameter, EditorRequired] public TagCategory Category { get; set; }

        private List<ChipModel<TagDTO>> _chipModels = default!;
        [Parameter, EditorRequired] public List<ChipModel<TagDTO>> ChipModels { get; set; } = default!;
        [Parameter] public EventCallback<List<ChipModel<TagDTO>>> ChipModelsChanged { get; set; }

        protected override async Task OnParametersSetAsync() {
            if (_chipModels != ChipModels) {
                _chipModels = ChipModels;
                await ChipModelsChanged.InvokeAsync(ChipModels);
            }
        }


        private TagDTO? _searchValue;
        public TagDTO? SearchValue {
            get => _searchValue;
            set {
                _searchValue = value;
                if (value != null) {
                    ChipModel<TagDTO>? chipModel = ChipModels.Find(m => m.Value.Id == value.Id);
                    if (chipModel == null) {
                        // create new ChipModel
                        ChipModels.Add(new ChipModel<TagDTO> { Value = value });
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
            ChipModels.Remove(mudChip.Value!);
        }

        private async Task<IEnumerable<TagDTO>> Search(string text, CancellationToken ct) {
            IEnumerable<Tag> tags = await TagService.GetTagsAsync(Category, 8, text, ct);
            return tags.Select(tag => tag.ToDTO());
        }
    }
}
