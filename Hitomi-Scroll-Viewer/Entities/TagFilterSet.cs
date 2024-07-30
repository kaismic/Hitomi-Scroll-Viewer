using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Hitomi_Scroll_Viewer.Entities
{
    [Index(nameof(TagFilterSetName))]
    internal class TagFilterSet
    {
        public const int TAG_FILTER_SET_NAME_MAX_LEN = 100;

        [MaxLength(TAG_FILTER_SET_NAME_MAX_LEN)]
        public string TagFilterSetName { get; set; }

        [MaxLength(7)] // TagFilter.CATEGORIES.Length
        public virtual ICollection<TagFilter> TagFilters { get; set; } = [];
    }
}
