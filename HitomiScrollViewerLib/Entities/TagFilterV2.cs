using System.Collections.Generic;
using System.Linq;
using static HitomiScrollViewerLib.SharedResources;

namespace HitomiScrollViewerLib.Entities {
    internal class TagFilterV2 {
        public Dictionary<string, IEnumerable<string>> includeTags = [];
        public Dictionary<string, IEnumerable<string>> excludeTags = [];
        internal TagFilterSet[] ToTagFilterSet(string name) {
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

            foreach (var pair in includeTags) {
                includeTagFilters[TagFilterV3.CATEGORY_INDEX_MAP[pair.Key]].Tags = pair.Value.ToList();
            }
            foreach (var pair in excludeTags) {
                excludeTagFilters[TagFilterV3.CATEGORY_INDEX_MAP[pair.Key]].Tags = pair.Value.ToList();
            }

            return [
                new TagFilterSet() {
                    Name = name + " - " + TEXT_INCLUDE,
                    TagFilters = includeTagFilters
                },
                new TagFilterSet() {
                    Name = name + " - " + TEXT_EXCLUDE,
                    TagFilters = excludeTagFilters
                }
            ];
        }
    }

}
