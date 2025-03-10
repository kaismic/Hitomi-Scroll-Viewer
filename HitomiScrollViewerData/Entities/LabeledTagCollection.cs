using HitomiScrollViewerData.DTOs;
using System.ComponentModel.DataAnnotations;

namespace HitomiScrollViewerData.Entities
{
    public class LabeledTagCollection
    {
        public long Id { get; set; }
        public required TagCategory Category { get; set; }
        public required ICollection<Tag> IncludeTags { get; set; }
        public required ICollection<Tag> ExcludeTags { get; set; }
        [Required] public SearchFilter SearchFilter { get; set; } = null!;

        public LabeledTagCollectionDTO ToDTO() => new() {
            Id = Id,
            Category = Category,
            IncludeTags = [.. IncludeTags.Select(t => t.ToDTO())],
            ExcludeTags = [.. ExcludeTags.Select(t => t.ToDTO())],
            SearchFilterId = SearchFilter.Id
        };
    }
}
