using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Hitomi_Scroll_Viewer.MainWindowComponent.SearchPageComponent.TagFilterSetSelectorComponent {
    public sealed partial class TagFilterCheckBox : CheckBox
    {
        internal int Index { get; set; }
        internal readonly string TagFilterSetName;
        public TagFilterCheckBox(int index, string tagFilterSetName, RoutedEventHandler checkedEventHandler, RoutedEventHandler uncheckedEventHandler)
        {
            Index = index;
            TagFilterSetName = tagFilterSetName;
            InitializeComponent();
            Checked += checkedEventHandler;
            Unchecked += uncheckedEventHandler;
        }
    }
}
