using Microsoft.EntityFrameworkCore;
using System;

namespace HitomiScrollViewerLib.Entities {
    [Index(nameof(DisplayName))]
    public class GalleryLanguage {
        public int Id { get; set; }
        public string DisplayName { get; set; }
        private string _searchParamValue;
        public string SearchParamValue {
            get => _searchParamValue;
            set {
                _searchParamValue = value;
                DisplayName = value[..1].ToUpper() + value[1..];
            }
        }
    }
}
