using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.ApplicationModel.Resources;
using static HitomiScrollViewerLib.SharedResources;

namespace HitomiScrollViewerLib.Models {
    public class ScrollDirection {
        private static readonly ResourceMap _resourceMap = MainResourceMap.GetSubtree(typeof(ScrollDirection).Name);
        private Orientation _value;
        public required Orientation Value {
            get => _value;
            set {
                _value = value;
                DisplayText = _resourceMap.GetValue(nameof(value)).ValueAsString;
            }
        }
        public string DisplayText { get; private set; }
    }
}
