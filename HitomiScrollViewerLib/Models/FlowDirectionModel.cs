using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;

namespace HitomiScrollViewerLib.Models {
    public class FlowDirectionModel {
        private static readonly string SUBTREE_NAME = typeof(FlowDirection).Name;
        public required FlowDirection Value { get; init; }
        public string DisplayText => Value.ToString().GetLocalized(SUBTREE_NAME);
    }
}
