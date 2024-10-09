using HitomiScrollViewerLib.Entities;
using System.Collections.Generic;

namespace HitomiScrollViewerLib.Models {
    public class InExcludeTagCollection {
        public string CategoryLabel { get; init; }
        public ICollection<Tag> IncludeTags { get; init; }
        public ICollection<Tag> ExcludeTags { get; init; }
    }
}
