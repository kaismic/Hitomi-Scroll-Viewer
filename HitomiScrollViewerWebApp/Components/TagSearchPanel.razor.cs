﻿using HitomiScrollViewerData.DTOs;
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
        [Parameter, EditorRequired] public ICollection<ChipModel<TagDTO>> ChipModels { get; set; } = default!;

        public TagDTO? SearchValue { get; set; }
        private async Task OnSearchValueChanged(TagDTO? value) {
            if (value != null) {
                ChipModel<TagDTO>? chipModel = ChipModels.FirstOrDefault(m => m.Value.Id == value.Id);
                if (chipModel == null) {
                    // create new ChipModel
                    ChipModels.Add(new ChipModel<TagDTO> { Value = value });
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
        }

        private async Task<IEnumerable<TagDTO>> Search(string text, CancellationToken ct) {
            IEnumerable<Tag> tags = await TagService.GetTagsAsync(Category, 8, text, ct);
            return tags.Select(tag => tag.ToDTO());
        }
    }
}
