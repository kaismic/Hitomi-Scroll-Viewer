using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Hitomi_Scroll_Viewer.MainWindowComponent.SearchPageComponent.TagFilterSetControlComponent {
    public sealed partial class TagFilterCheckBox : CheckBox
    {
        public readonly string TagFilterSetName;
        public TagFilterCheckBox(string tagFilterSetName, RoutedEventHandler checkedEventHandler, RoutedEventHandler uncheckedEventHandler)
        {
            TagFilterSetName = tagFilterSetName;
            InitializeComponent();
            Checked += checkedEventHandler;
            Unchecked += uncheckedEventHandler;
        }
    }
}
