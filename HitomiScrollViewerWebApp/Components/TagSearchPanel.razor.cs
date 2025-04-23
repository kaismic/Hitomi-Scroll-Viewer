﻿using HitomiScrollViewerData;
using HitomiScrollViewerData.DTOs;
using HitomiScrollViewerData.Entities;
using HitomiScrollViewerWebApp.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor;

namespace HitomiScrollViewerWebApp.Components {
    public partial class TagSearchPanel : ComponentBase {
        private const string JAVASCRIPT_FILE = $"./Components/{nameof(TagSearchPanel)}.razor.js";
        private IJSObjectReference? _jsModule;

        [Parameter, EditorRequired] public TagCategory Category { get; set; }
        [Parameter, EditorRequired] public List<ChipModel<TagDTO>> ChipModels { get; set; } = default!;
        [Parameter] public EventCallback<AdvancedCollectionChangedEventArgs<ChipModel<TagDTO>>> ChipModelsChanged { get; set; } 
        private IReadOnlyCollection<ChipModel<TagDTO>> _selectedChipModels = [];

        public TagDTO? SearchValue { get; set; }
        private async Task OnSearchValueChanged(TagDTO? value) {
            if (value != null) {
                ChipModel<TagDTO>? chipModel = ChipModels.FirstOrDefault(m => m.Value.Id == value.Id);
                if (chipModel == null) {
                    // create new ChipModel
                    ChipModel<TagDTO> newChipModel = new() { Value = value };
                    ChipModels.Add(newChipModel);
                    await ChipModelsChanged.InvokeAsync(new(AdvancedCollectionChangedAction.AddSingle, newChipModel));
                    SearchValue = null;
                } else {
                    // already exists in ChipModels
                    _jsModule ??= await JSRuntime.InvokeAsync<IJSObjectReference>("import", JAVASCRIPT_FILE);
                    await _jsModule.InvokeVoidAsync("scrollToElement", chipModel.Id);
                }
            }
        }

        private void HandleClosed(MudChip<ChipModel<TagDTO>> mudChip) {
            ChipModels.Remove(mudChip.Value!);
            ChipModelsChanged.InvokeAsync(new(AdvancedCollectionChangedAction.RemoveSingle, mudChip.Value!));
        }

        private async Task<IEnumerable<TagDTO>> Search(string text, CancellationToken ct) {
            IEnumerable<Tag> tags = await TagService.GetTagsAsync(Category, 8, text, ct);
            return tags.Select(tag => tag.ToDTO());
        }

        private void OnKeyDown(KeyboardEventArgs args) {
            switch (args.Key) {
                case "Backspace" or "Delete":
                    List<ChipModel<TagDTO>> removingModels = [];
                    foreach (var model in _selectedChipModels) {
                        removingModels.Add(model);
                        ChipModels.Remove(model);
                    }
                    ChipModelsChanged.InvokeAsync(new(AdvancedCollectionChangedAction.RemoveMultiple, removingModels));
                    break;
            }
        }
    }
}
