using HitomiScrollViewerData.DbContexts;
using HitomiScrollViewerData.Entities;

namespace HitomiScrollViewerData.DTOs {
    public class GallerySyncDTO {
        public required int Id { get; set; }
        public required string Title { get; set; }
        public string? JapaneseTitle { get; set; }
        public DateTimeOffset Date { get; set; }
        public required int[] SceneIndexes { get; set; }
        public required int[] Related { get; set; }
        public required int GalleryLanguageId { get; set; }
        public required int GalleryTypeId { get; set; }
        public required IEnumerable<ImageInfoSyncDTO> Files { get; set; }
        public required IEnumerable<int> TagIds { get; set; }

        public Gallery ToGallery(HitomiContext context) => new() {
            Id = Id,
            Title = Title,
            JapaneseTitle = JapaneseTitle,
            Date = Date,
            SceneIndexes = SceneIndexes,
            Related = Related,
            GalleryLanguage = context.GalleryLanguages.Find(GalleryLanguageId)!,
            GalleryType = context.GalleryTypes.Find(GalleryTypeId)!,
            LastDownloadTime = DateTime.UtcNow,
            Files = [.. Files.Select(f => f.ToImageInfo())],
            Tags = [.. TagIds.Select(id => context.Tags.Find(id)!)]
        };
    }
}
