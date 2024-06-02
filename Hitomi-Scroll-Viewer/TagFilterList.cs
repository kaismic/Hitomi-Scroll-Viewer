using System.Collections.Generic;
using System.Linq;

namespace Hitomi_Scroll_Viewer {
    public class TagFilterList {
        public static readonly string[] CATEGORIES = [
            "language", "female", "male", "artist", "character", "group", "series", "type", "tag"
        ];

        public Dictionary<string, HashSet<string>> includeTags = [];
        public Dictionary<string, HashSet<string>> excludeTags = [];
        public TagFilterList() {
            foreach (string tag in CATEGORIES) {
                includeTags[tag] = [];
                excludeTags[tag] = [];
            }
        }

        public string GetIncludeExcludeOverlap() {
            Dictionary<string, HashSet<string>> overlapDict = [];
            foreach (string category in CATEGORIES) {
                HashSet<string> overlaps = includeTags[category].Intersect(excludeTags[category]).ToHashSet();
                if (overlaps.Count > 0) {
                    overlapDict[category] = overlaps;
                }
            }
            return overlapDict.Aggregate(
                "",
                (result, pair) => result += pair.Key + ": " + string.Join(", ", pair.Value) + '\n'
            );
        }
    }
}