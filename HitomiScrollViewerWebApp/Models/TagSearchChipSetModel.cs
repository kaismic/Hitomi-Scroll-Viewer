using HitomiScrollViewerData.DTOs;
using HitomiScrollViewerData.Entities;

namespace HitomiScrollViewerWebApp.Models {
    public class TagSearchChipSetModel : SearchChipSetModel<TagDTO> {
        public required TagCategory TagCategory { get; init; }
    }
}
