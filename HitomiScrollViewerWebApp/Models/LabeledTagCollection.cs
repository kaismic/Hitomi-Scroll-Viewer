using HitomiScrollViewerData.DTOs;
using HitomiScrollViewerData.Entities;

namespace HitomiScrollViewerWebApp.Models {
    public class LabeledTagCollection {
        public required TagCategory Category { get; init; }
        public required IEnumerable<TagDTO> IncludeTags { get; init; }
        public required IEnumerable<TagDTO> ExcludeTags { get; init; }
    }
}