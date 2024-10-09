using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using static HitomiScrollViewerLib.SharedResources;

namespace HitomiScrollViewerLib.Entities {
    [Index(nameof(IsAll))]
    [Index(nameof(SearchParamValue))]
    [Index(nameof(LocalName))]
    public class GalleryLanguage {
        public int Id { get; private set; }
        public required bool IsAll { get; set; }
        private string _searchParamValue;
        public required string SearchParamValue {
            get {
                if (IsAll) {
                    throw new InvalidOperationException($"{nameof(SearchParamValue)} must not be accessed when {nameof(IsAll)} is true");
                }
                return _searchParamValue;
            }
            init => _searchParamValue = value;
        }

        private string _localName;
        public string LocalName {
            get {
                if (IsAll) {
                    return TEXT_ALL;
                }
                return _localName;
            }
            init {
                _localName = value;
            }
        }

        public ICollection<Gallery> Galleries { get; } = [];
    }
}
