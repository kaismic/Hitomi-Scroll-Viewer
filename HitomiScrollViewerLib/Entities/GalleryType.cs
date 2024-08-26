using System.Collections.Generic;

namespace HitomiScrollViewerLib.Entities {
    public class GalleryType {
        public static IEnumerable<string> DisplayNames { get; } = [
            "Any", "Doujinshi", "Manga", "ArtistCG", "GameCG", "Imageset"
        ];
        private string _displayName;
        public string DisplayName {
            get => _displayName;
            set {
                _displayName = value;
                SearchParamValue = value.ToLower();
            }
        }
        public string SearchParamValue { get; set; }
    }
}
