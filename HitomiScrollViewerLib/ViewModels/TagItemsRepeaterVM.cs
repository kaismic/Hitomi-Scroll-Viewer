using System.Collections.Generic;

namespace HitomiScrollViewerLib.ViewModels {
    public class TagItemsRepeaterVM {
        public required string CategoryLabel { get; set; }
        public required List<string> TagDisplayString { get; set; }
        public double CategoryFontSize { get; set; }
        public double TagFontSize { get; set; }
    }
}