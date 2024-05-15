using System.Collections.Generic;

namespace Hitomi_Scroll_Viewer {
    public class SearchTag {
        public static readonly string[] CATEGORIES = [
            "language", "female", "male", "artist", "character", "group", "series", "type", "tag"
        ];

        public Dictionary<string, string[]> includeTags = [];
        public Dictionary<string, string[]> excludeTags = [];
        public SearchTag() {
            foreach (string tag in CATEGORIES) {
                includeTags[tag] = [];
                excludeTags[tag] = [];
            }
        }
    }
}