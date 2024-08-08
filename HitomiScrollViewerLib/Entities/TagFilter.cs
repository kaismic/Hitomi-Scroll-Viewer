using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace HitomiScrollViewerLib.Entities {
    public class TagFilter {
        public static readonly string[] CATEGORIES = [
            "language", "female", "male", "artist", "character", "group", "series", "type", "tag"
        ];
        public static readonly Dictionary<string, int> CATEGORY_INDEX_MAP =
            CATEGORIES
            .Select((category, i) => new KeyValuePair<string, int>(category, i))
            .ToDictionary();

        public virtual TagFilterSet TagFilterSet { get; set; }
        public long Id { get; set; }
        [MaxLength(9)]
        public string Category { get; set; }
        public List<string> Tags { get; set; }
    }
}