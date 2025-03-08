using HitomiScrollViewerData.Entities;

namespace HitomiScrollViewerData.DTOs {
    public class TagDTO {
        public required int Id { get; set; }
        public required TagCategory Category { get; set; }
        public required string Value { get; set; }
        public required string SearchParamValue { get; set; }
        public required int GalleryCount { get; set; }

        public Tag ToTag() => new() {
            Id = Id,
            Category = Category,
            Value = Value,
            GalleryCount = GalleryCount
        };
    }
}
