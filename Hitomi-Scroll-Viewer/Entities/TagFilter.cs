using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Hitomi_Scroll_Viewer.Entities
{
    internal class TagFilter
    {
        public static readonly string[] CATEGORIES = [
            "language", "female", "male", "artist", "character", "group", "series", "type", "tag"
        ];

        [MaxLength(TagFilterSet.TAG_FILTER_SET_NAME_MAX_LEN)]
        public string TagFilterSetName { get; set; }
        [MaxLength(9)]
        public string Category { get; set; }
        public IEnumerable<string> TagFilters { get; set; }
    }
}