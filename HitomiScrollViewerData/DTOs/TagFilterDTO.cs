using HitomiScrollViewerData.Entities;

namespace HitomiScrollViewerData.DTOs {
    public class TagFilterDTO {
        public int Id { get; set; }
        public required string Name { get; set; }
        public TagFilter ToTagFilter() => new() { Id = Id, Name = Name };
    }
}
