using HitomiScrollViewerData.DTOs;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace HitomiScrollViewerData.Entities {
    [Index(nameof(Title))]
    [Index(nameof(Date))]
    [Index(nameof(LastDownloadTime))]
    public class Gallery {
        public int Id { get; set; }
        public required string Title { get; set; }
        public string? JapaneseTitle { get; set; }
        public DateTimeOffset Date { get; set; }
        public required int[] SceneIndexes { get; set; }
        [MaxLength(45)] public required int[] Related { get; set; } // 7 digits * 5 items + "[]" + ", " * (5 - 1)
        public DateTime LastDownloadTime { get; set; }
        [Required] public required GalleryLanguage Language { get; set; }
        [Required] public required GalleryType Type { get; set; }
        public required ICollection<GalleryImage> GalleryImages { get; set; }
        public required ICollection<Tag> Tags { get; set; }

        public GalleryDownloadDTO ToDownloadDTO(int count) => new() {
            Id = Id,
            Title = Title,
            GalleryImagesCount = count,
        };

        public GalleryFullDTO ToFullDTO() => new() {
            Id = Id,
            Title = Title,
            JapaneseTitle = JapaneseTitle,
            Date = Date,
            SceneIndexes = SceneIndexes,
            Related = Related,
            LastDownloadTime = LastDownloadTime,
            Language = Language.ToDTO(),
            Type = Type.ToDTO(),
            GalleryImages = [.. GalleryImages.Select(g => g.ToDTO())],
            Tags = [.. Tags.Select(t => t.ToDTO())]
        };
    }
}
