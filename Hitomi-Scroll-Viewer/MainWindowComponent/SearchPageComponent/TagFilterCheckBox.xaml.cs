using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Hitomi_Scroll_Viewer.MainWindowComponent.SearchPageComponent {
    public sealed partial class TagFilterCheckBox : CheckBox
    {
        public readonly string TagFilterName;
        public TagFilterCheckBox(string tagName, RoutedEventHandler checkedEventHandler, RoutedEventHandler uncheckedEventHandler)
        {
            TagFilterName = tagName;
            InitializeComponent();
            Checked += checkedEventHandler;
            Unchecked += uncheckedEventHandler;
        }
    }
}
