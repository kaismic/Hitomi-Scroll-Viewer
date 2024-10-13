using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml.Controls;

namespace HitomiScrollViewerLib.Models {
    public class ScrollDirection {
        private static readonly string SUBTREE_NAME = typeof(ScrollDirection).Name;
        public required Orientation Value { get; init; }
        public string DisplayText => Value.ToString().GetLocalized(SUBTREE_NAME);
    }
}
