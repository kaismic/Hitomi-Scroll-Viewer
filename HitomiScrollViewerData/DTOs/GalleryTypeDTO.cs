using HitomiScrollViewerData.Entities;

namespace HitomiScrollViewerData.DTOs
{
    public class GalleryTypeDTO
    {
        public required int Id { get; set; }
        public required bool IsAll { get; set; }
        public required string Value { get; set; }

        public GalleryType ToEntity() => new() {
            Id = Id,
            IsAll = IsAll,
            Value = Value
        };
    }
}
