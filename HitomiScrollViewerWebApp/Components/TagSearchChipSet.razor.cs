using HitomiScrollViewerData.DTOs;
using HitomiScrollViewerWebApp.Models;
using Microsoft.JSInterop;

namespace HitomiScrollViewerWebApp.Components {
    public class TagSearchChipSet : SearchChipSet<TagDTO> {
        private TagSearchChipSetModel _model = null!;
        public override SearchChipSetModel<TagDTO> Model {
            get => _model;
            init => _model = (TagSearchChipSetModel)value;
        }

        private TagDTO? _searchValue;
        protected override TagDTO? SearchValue {
            get => _searchValue;
            set {
                _searchValue = value;
                if (value != null) {
                    if (Model.ChipModels.Any(m => m.Value.Id == value.Id)) {
                        // already exists in ChipModels
#pragma warning disable CA2012 // Use ValueTasks correctly
                        _ = JSRuntime.InvokeVoidAsync("scrollToElement", value.Id);
#pragma warning restore CA2012 // Use ValueTasks correctly
                    } else {
                        Model.ChipModels.Add(new SearchChipModel<TagDTO> { Value = value });
                        _searchValue = null;
                    }
                }
            }
        }
    }
}
