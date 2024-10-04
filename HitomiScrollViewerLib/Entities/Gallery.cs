using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HitomiScrollViewerLib.Entities {
    [Index(nameof(Title))]
    [Index(nameof(DownloadTime))]
    public class Gallery {
        public int Id { get; set; }
        public string Title { get; set; }
        public string JapaneseTitle { get; set; }
        public DateTimeOffset Date { get; set; }
        public int[] SceneIndexes { get; set; }
        public int[] Related { get; set; }
        public DateTime DownloadTime { get; init; } = DateTime.UtcNow;
        [Required]
        public virtual GalleryTypeEntity GalleryType { get; set; }
        [Required]
        public virtual GalleryLanguage GalleryLanguage { get; set; }
        public virtual ICollection<ImageInfo> Files { get; set; }
        public virtual ICollection<Tag> Tags { get; set; }
    }
}
