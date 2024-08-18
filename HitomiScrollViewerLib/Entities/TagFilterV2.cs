using System.Collections.Generic;
using System.Linq;
using static HitomiScrollViewerLib.SharedResources;

namespace HitomiScrollViewerLib.Entities {
    internal class TagFilterV2 {
        public Dictionary<string, IEnumerable<string>> includeTags = [];
        public Dictionary<string, IEnumerable<string>> excludeTags = [];
        internal List<TagFilterSet> ToTagFilterSet(string name) {
            List<TagFilterV3> includeTagFilters = TagFilterV3.CATEGORIES
                .Select(
                    category => new TagFilterV3() {
                        Category = category,
                    }
                )
                .ToList();
            List<TagFilterV3> excludeTagFilters = TagFilterV3.CATEGORIES
                .Select(
                    category => new TagFilterV3() {
                        Category = category,
                    }
                )
                .ToList();

            List<TagFilterSet> result = [];

            bool hasAnyTags = false;
            foreach (var pair in includeTags) {
                List<string> tags = pair.Value.ToList();
                hasAnyTags |= tags.Count != 0;
                includeTagFilters[TagFilterV3.CATEGORY_INDEX_MAP[pair.Key]].Tags = tags;
            }
            if (hasAnyTags) {
                result.Add(
                    new TagFilterSet() {
                        Name = name + " - " + TEXT_INCLUDE,
                        TagFilters = includeTagFilters
                    }
                );
            }

            hasAnyTags = false;
            foreach (var pair in excludeTags) {
                List<string> tags = pair.Value.ToList();
                hasAnyTags |= tags.Count != 0;
                excludeTagFilters[TagFilterV3.CATEGORY_INDEX_MAP[pair.Key]].Tags = tags;
            }
            if (hasAnyTags) {
                result.Add(
                    new TagFilterSet() {
                        Name = name + " - " + TEXT_INCLUDE,
                        TagFilters = includeTagFilters
                    }
                );
            }

            return result;
        }
    }

}
