using HitomiScrollViewerLib.Entities;
using System.Collections.Generic;

namespace HitomiScrollViewerLib.Models {
    public class InExcludeTagCollection {
        public string CategoryLabel { get; init; }
        public List<Tag> IncludeTags { get; init; }
        public List<Tag> ExcludeTags { get; init; }
    }
}
