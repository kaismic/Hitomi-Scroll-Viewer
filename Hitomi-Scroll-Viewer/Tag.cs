using System.Collections.Generic;

namespace Hitomi_Scroll_Viewer {
    public class Tag {
        public static readonly string[] CATEGORIES = {
            "language", "female", "male", "artist", "character", "group", "series", "type", "tag"
        };

        public Dictionary<string, string[]> includeTags = new();
        public Dictionary<string, string[]> excludeTags = new();
        public Tag() {
            foreach (string tag in CATEGORIES) {
                includeTags[tag] = System.Array.Empty<string>();
                excludeTags[tag] = System.Array.Empty<string>();
            }
        }
    }
}