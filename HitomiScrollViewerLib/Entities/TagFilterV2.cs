using HitomiScrollViewerLib.DbContexts;
using System.Collections.Generic;
using System.Linq;
using static HitomiScrollViewerLib.SharedResources;

namespace HitomiScrollViewerLib.Entities {
    internal class TagFilterV2 {
        private static readonly Dictionary<string, Category> INV_CATEGORY_PROP_KEY_DICT = new() {
            { "tag", Category.Tag },
            { "male", Category.Male },
            { "female", Category.Female },
            { "artist", Category.Artist },
            { "group", Category.Group },
            { "character", Category.Character },
            { "series", Category.Series }
        };

        public Dictionary<string, IEnumerable<string>> IncludeTags { get; set; }
        public Dictionary<string, IEnumerable<string>> ExcludeTags { get; set; }
        internal List<TagFilterSet> ToTagFilterSet(string name) {
            List<TagFilterSet> result = [];

            bool hasAnyTags = false;
            List<Tag> includeTFSTags = [];
            foreach (var kvp in IncludeTags) {
                if (kvp.Key == "language" || kvp.Key == "type") {
                    continue;
                }
                List<string> tagValues = kvp.Value.ToList();
                hasAnyTags |= tagValues.Count != 0;
                includeTFSTags.AddRange(
                    tagValues.Select(tagValue =>
                        HitomiContext.Main.Tags
                        .Where(tag => tag.Value.Equals(tagValue, System.StringComparison.CurrentCultureIgnoreCase) && tag.Category == INV_CATEGORY_PROP_KEY_DICT[kvp.Key])
                        .First()
                    )
                );
            }
            if (hasAnyTags) {
                result.Add(
                    new TagFilterSet() {
                        Name = name + " - " + TEXT_INCLUDE,
                        Tags = includeTFSTags
                    }
                );
            }

            hasAnyTags = false;
            List<Tag> excludeTFSTags = [];
            foreach (var kvp in IncludeTags) {
                if (kvp.Key == "language" || kvp.Key == "type") {
                    continue;
                }
                List<string> tagValues = kvp.Value.ToList();
                hasAnyTags |= tagValues.Count != 0;
                excludeTFSTags.AddRange(
                    tagValues.Select(tagValue =>
                        HitomiContext.Main.Tags
                        .Where(tag => tag.Value.Equals(tagValue, System.StringComparison.CurrentCultureIgnoreCase) && tag.Category == INV_CATEGORY_PROP_KEY_DICT[kvp.Key])
                        .First()
                    )
                );
            }
            if (hasAnyTags) {
                result.Add(
                    new TagFilterSet() {
                        Name = name + " - " + TEXT_INCLUDE,
                        Tags = excludeTFSTags
                    }
                );
            }

            return result;
        }
    }

}
