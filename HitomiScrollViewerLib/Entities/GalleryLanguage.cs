using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;

namespace HitomiScrollViewerLib.Entities {
    [Index(nameof(LocalName))]
    [Index(nameof(SearchParamValue))]
    public class GalleryLanguage {
        public int Id { get; set; }
        public string SearchParamValue { get; set; }
        public string LocalName { get; set; }
        public ICollection<Gallery> Galleries { get; set; }
    }
}
