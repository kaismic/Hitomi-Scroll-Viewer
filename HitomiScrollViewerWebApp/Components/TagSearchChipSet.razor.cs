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
                    SearchChipModel<TagDTO>? chipModel = Model.ChipModels.Find(m => m.Value.Id == value.Id);
                    if (chipModel == null) {
                        // create new ChipModel
                        Model.ChipModels.Add(new SearchChipModel<TagDTO> { Value = value });
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
    }
}
