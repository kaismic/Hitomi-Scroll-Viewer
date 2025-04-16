using HitomiScrollViewerData.Entities;

namespace HitomiScrollViewerData.DTOs
{
    public class GalleryTypeDTO
    {
        public int Id { get; set; }
        public bool IsAll { get; set; }
        public string Value { get; set; } = "";

        public GalleryType ToEntity() => new() {
            Id = Id,
            IsAll = IsAll,
            Value = Value
        };
    }
}
