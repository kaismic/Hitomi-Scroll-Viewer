using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;

namespace HitomiScrollViewerLib.Entities {
    [Index(nameof(LocalName))]
    [Index(nameof(EnglishName))]
    public class GalleryLanguage {
        public int Id { get; set; }
        private string _searchParamValue;
        public string SearchParamValue {
            get => _searchParamValue;
            set {
                _searchParamValue = value;
                EnglishName = value[..1].ToUpper() + value[1..];
            }
        }
        public string LocalName { get; set; }
        public string EnglishName { get; set; }
        public virtual ICollection<Gallery> Galleries { get; set; }
    }
}
