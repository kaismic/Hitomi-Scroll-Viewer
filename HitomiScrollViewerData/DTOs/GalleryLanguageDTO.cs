using HitomiScrollViewerData.Entities;

namespace HitomiScrollViewerData.DTOs {
    public class GalleryLanguageDTO {
        public required int Id { get; set; }
        public required bool IsAll { get; set; }
        public required string EnglishName { get; set; }
        public required string LocalName { get; set; }
        public GalleryLanguage ToEntity() => new() {
            Id = Id,
            IsAll = IsAll,
            EnglishName = EnglishName,
            LocalName = LocalName
        };
    }
}
