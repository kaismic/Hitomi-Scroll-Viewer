﻿using HitomiScrollViewerData.DTOs;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace HitomiScrollViewerData.Entities {
    [Index(nameof(Title))]
    [Index(nameof(Date))]
    [Index(nameof(LastDownloadTime))]
    public class Gallery {
        public int Id { get; set; }
        //public string ImageFilesDirectory => Path.Combine(Constants.IMAGE_DIR_V3, Id.ToString());
        public string Title { get; set; }
        public string JapaneseTitle { get; set; }
        public DateTimeOffset Date { get; set; }
        public int[] SceneIndexes { get; set; }
        [MaxLength(45)] // 7 digits * 5 items + "[]" + ", " * (5 - 1)
        public int[] Related { get; set; }
        public DateTime LastDownloadTime { get; set; }
        [Required]
        public GalleryLanguage GalleryLanguage { get; set; }
        [Required]
        public GalleryType GalleryType { get; set; }
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
