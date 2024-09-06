﻿using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace HitomiScrollViewerLib.Entities {
    [Index(nameof(Title))]
    [Index(nameof(LastDownloadTime))]
    public class Gallery {
        public int Id { get; set; }
        public string Title { get; set; }
        public string JapaneseTitle { get; set; }
        public string Date { get; set; }
        public int[] SceneIndexes { get; set; }
        public int[] Related { get; set; }
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime LastDownloadTime { get; set; }

        public virtual GalleryType GalleryType { get; set; }
        public virtual GalleryLanguage GalleryLanguage { get; set; }
        public virtual ICollection<ImageInfo> Files { get; set; }
        public virtual ICollection<Tag> Tags { get; set; }
    }
}
