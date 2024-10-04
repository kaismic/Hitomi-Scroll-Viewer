using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.ApplicationModel.Resources;
using static HitomiScrollViewerLib.SharedResources;

namespace HitomiScrollViewerLib.Models {
    public class ScrollDirection {
        private static readonly ResourceMap _resourceMap = MainResourceMap.GetSubtree(typeof(ScrollDirection).Name);
        public required Orientation Value { get; init; }
        public string DisplayText => _resourceMap.GetValue(Value.ToString()).ValueAsString;
    }
}
