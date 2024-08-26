using System.Collections.Generic;

namespace HitomiScrollViewerLib.Entities {
    public class GalleryType {
        public static IEnumerable<string> DisplayNames { get; } = [
            "Any", "Doujinshi", "Manga", "ArtistCG", "GameCG", "Imageset"
        ];
        public string DisplayName { get; set; }
        public string SearchParamValue { get; set; }
    }
}
