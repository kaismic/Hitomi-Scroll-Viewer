using HitomiScrollViewerData.Entities;

namespace HitomiScrollViewerData.DTOs
{
    public class LabeledTagCollectionDTO
    {
        public required long Id { get; set; }
        public required TagCategory Category { get; set; }
        public required ICollection<TagDTO> IncludeTags { get; set; }
        public required ICollection<TagDTO> ExcludeTags { get; set; }
        public required int SearchFilterId { get; set; }

        public LabeledTagCollection ToEntity() => new() {
            Id = Id,
            Category = Category,
            IncludeTags = [.. IncludeTags.Select(t => t.ToEntity())],
            ExcludeTags = [.. ExcludeTags.Select(t => t.ToEntity())]
        };
    }
}
