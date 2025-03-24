﻿using HitomiScrollViewerData.DTOs;
using HitomiScrollViewerWebApp.Models;
using Microsoft.AspNetCore.Components;

namespace HitomiScrollViewerWebApp.Components {
    public partial class DialogTagFilterSelector : ComponentBase, IDialogContent {
        private TagFilterSelector _tagFilterSelector = default!;
        [Parameter, EditorRequired] public List<ChipModel<TagFilterDTO>> ChipModels { get; set; } = default!;
        public Action OnSubmit { get; set; } = () => { };

        public event Action<bool>? DisableActionButtonChanged;
        public object GetResult() => _tagFilterSelector.SelectedChipModels;
        public bool Validate() => true;

        protected override void OnAfterRender(bool firstRender) {
            if (firstRender) {
                _tagFilterSelector.SelectedChipModelsChanged += (collection) =>
                    DisableActionButtonChanged?.Invoke(collection != null && collection.Count == 0);
            }
        }
    }
}
