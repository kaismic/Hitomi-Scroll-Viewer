using Microsoft.UI.Xaml;
using Microsoft.Windows.ApplicationModel.Resources;
using static HitomiScrollViewerLib.SharedResources;

namespace HitomiScrollViewerLib.Models {
    public class FlowDirectionModel {
        private static readonly ResourceMap _resourceMap = MainResourceMap.GetSubtree(nameof(FlowDirection));
        public required FlowDirection Value { get; init; }
        public string DisplayText => _resourceMap.GetValue(Value.ToString()).ValueAsString;
    }
}
