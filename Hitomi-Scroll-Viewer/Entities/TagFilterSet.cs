using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using static Hitomi_Scroll_Viewer.Entities.TagFilter;

namespace Hitomi_Scroll_Viewer.Entities
{
    [Index(nameof(Name))]
    internal class TagFilterSet
    {
        public const int TAG_FILTER_SET_NAME_MAX_LEN = 100;

        [MaxLength(TAG_FILTER_SET_NAME_MAX_LEN)]
        public string Name { get; set; }

        public TagFilter[] TagFilters { get; set; } =
            CATEGORIES
            .Select(category => new TagFilter() { Category = category })
            .ToArray();
    }
}
