using HitomiScrollViewerData.DTOs;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace HitomiScrollViewerData.Entities {
    [Index(nameof(Title))]
    [Index(nameof(Date))]
    [Index(nameof(LastDownloadTime))]
    public class Gallery {
        public int Id { get; set; }
        //public string ImageFilesDirectory => Path.Combine(Constants.IMAGE_DIR_V3, Id.ToString());
        public required string Title { get; set; }
        public string? JapaneseTitle { get; set; }
        public DateTimeOffset Date { get; set; }
        public required int[] SceneIndexes { get; set; }
        // 7 digits * 5 items + "[]" + ", " * (5 - 1)
        [MaxLength(45)] public required int[] Related { get; set; }
        public DateTime LastDownloadTime { get; set; }
        [Required] public required GalleryLanguage GalleryLanguage { get; set; }
        [Required] public required GalleryType GalleryType { get; set; }
        public required ICollection<ImageInfo> Files { get; set; }
        public required ICollection<Tag> Tags { get; set; }

        public GallerySyncDTO ToGallerySyncDTO() => new() {
            Id = Id,
            Title = Title,
            JapaneseTitle = JapaneseTitle,
            Date = Date,
            SceneIndexes = SceneIndexes,
            Related = Related,
            GalleryLanguageId = GalleryLanguage.Id,
            GalleryTypeId = GalleryType.Id,
            Files = Files.Select(f => f.ToImageInfoSyncDTO()),
            TagIds = Tags.Select(tag => tag.Id)
        };
    }
}
