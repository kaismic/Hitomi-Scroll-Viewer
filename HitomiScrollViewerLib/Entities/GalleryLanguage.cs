using Microsoft.EntityFrameworkCore;
using System;

namespace HitomiScrollViewerLib.Entities {
    [Index(nameof(DisplayName))]
    public class GalleryLanguage {
        public int Id { get; set; }
        public string DisplayName { get; set; }
        public string SearchParamValue { get; set; }
    }
}
