using HitomiScrollViewerData.DTOs;
using HitomiScrollViewerWebApp.Models;

namespace HitomiScrollViewerWebApp.Components {
    public class TagSearchChipSet : SearchChipSet<TagDTO> {

        private TagSearchChipSetModel _model = null!;
        public override SearchChipSetModel<TagDTO> Model {
            get => _model;
            init => _model = (TagSearchChipSetModel)value;
        }
    }
}
