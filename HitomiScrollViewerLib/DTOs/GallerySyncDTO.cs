using HitomiScrollViewerLib.DbContexts;
using HitomiScrollViewerLib.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HitomiScrollViewerLib.DTOs {
    public class GallerySyncDTO {
        public int Id { get; set; }
        public string Title { get; set; }
        public string JapaneseTitle { get; set; }
        public DateTimeOffset Date { get; set; }
        public int[] SceneIndexes { get; set; }
        public int[] Related { get; set; }
        public int GalleryLanguageId { get; set; }
        public int GalleryTypeId { get; set; }
        public required IEnumerable<ImageInfoSyncDTO> Files { get; set; }
        public IEnumerable<int> TagIds { get; set; }

        public Gallery ToGallery(HitomiContext context) => new() {
            Id = Id,
            Title = Title,
            JapaneseTitle = JapaneseTitle,
            Date = Date,
            SceneIndexes = SceneIndexes,
            Related = Related,
            GalleryLanguage = context.GalleryLanguages.Find(GalleryLanguageId),
            GalleryType = context.GalleryTypes.Find(GalleryTypeId),
            LastDownloadTime = DateTime.UtcNow,
            Files = [.. Files.Select(f => f.ToImageInfo())],
            Tags = [.. TagIds.Select(id => context.Tags.Find(id))]
        };
    }
}
