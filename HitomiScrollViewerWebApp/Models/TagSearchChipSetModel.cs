using HitomiScrollViewerData.DTOs;
using HitomiScrollViewerData.Entities;

namespace HitomiScrollViewerWebApp.Models {
    public class TagSearchChipSetModel {
        public required TagCategory TagCategory { get; init; }
        public string Label { get; set; } = "";
        public required Func<TagDTO, string> ToStringFunc { get; set; }
        public required Func<string, CancellationToken, Task<IEnumerable<TagDTO>>> SearchFunc { get; set; }
        public List<ChipModel<TagDTO>> ChipModels { get; set; } = [];
    }
}
