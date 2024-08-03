using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Hitomi_Scroll_Viewer.Entities {
    public class TagFilterSet {
        public const int TAG_FILTER_SET_NAME_MAX_LEN = 100;
        [Key]
        [MaxLength(TAG_FILTER_SET_NAME_MAX_LEN)]
        public string Name { get; set; }
        public virtual ICollection<TagFilter> TagFilters { get; set; }
    }
}
