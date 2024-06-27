using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Hitomi_Scroll_Viewer.MainWindowComponent.SearchPageComponent {
    public sealed partial class SearchFilterItem : CheckBox
    {
        public readonly string TagName;
        public SearchFilterItem(string tagName, RoutedEventHandler eventHandler)
        {
            TagName = tagName;
            InitializeComponent();
            Checked += eventHandler;
            Unchecked += eventHandler;
        }
    }
}
