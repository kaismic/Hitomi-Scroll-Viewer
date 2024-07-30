using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Hitomi_Scroll_Viewer.Entities
{
    internal class TagFilter
    {
        public static readonly string[] CATEGORIES = [
            "language", "female", "male", "artist", "character", "group", "series", "type", "tag"
        ];

        [MaxLength(9)]
        public string Category { get; set; }
        public IEnumerable<string> TagFilters { get; set; }
    }
}