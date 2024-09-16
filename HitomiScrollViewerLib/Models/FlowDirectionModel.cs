using Microsoft.UI.Xaml;
using Microsoft.Windows.ApplicationModel.Resources;
using static HitomiScrollViewerLib.SharedResources;

namespace HitomiScrollViewerLib.Models {
    public class FlowDirectionModel {
        private static readonly ResourceMap _resourceMap = MainResourceMap.GetSubtree(nameof(FlowDirection));
        private FlowDirection _value;
        public required FlowDirection Value {
            get => _value;
            init {
                _value = value;
                DisplayText = _resourceMap.GetValue(nameof(value)).ValueAsString;
            }
        }
        public string DisplayText { get; private set; }
    }
}
