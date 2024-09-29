using System.Collections.Generic;

namespace HitomiScrollViewerLib.ViewModels {
    public class TagItemsRepeaterVM {
        public string CategoryLabel { get; init; }
        public List<string> TagDisplayString { get; init; }
        public double CategoryFontSize { get; init; } = 12;
        public double TagFontSize { get; init; } = 10;
    }
}