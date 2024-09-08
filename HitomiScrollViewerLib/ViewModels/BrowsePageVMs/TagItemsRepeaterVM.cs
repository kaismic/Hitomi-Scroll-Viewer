using HitomiScrollViewerLib.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HitomiScrollViewerLib.ViewModels.BrowsePageVMs {
    public class TagItemsRepeaterVM {
        public required string CategoryLabel { get; set; }
        public required List<Tag> Tags { get; set; }
    }
}
