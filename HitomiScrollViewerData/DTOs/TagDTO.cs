using HitomiScrollViewerData.Entities;

namespace HitomiScrollViewerData.DTOs {
    public class TagDTO {
        public required int Id { get; set; }
        public required TagCategory Category { get; set; }

        public required string Value { get; set; }
        public required int GalleryCount { get; set; }
    }
}
