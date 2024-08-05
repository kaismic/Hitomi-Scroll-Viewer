using Hitomi_Scroll_Viewer.Entities;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.ComponentModel;

namespace Hitomi_Scroll_Viewer.MainWindowComponent.SearchPageComponent.TagFilterSetSelectorComponent {
    public sealed partial class TagFilterCheckBox : CheckBox {
        internal int Index { get; set; }
        internal TagFilterSet TagFilterSet { get; private set; }
        public TagFilterCheckBox(int index, TagFilterSet tagFilterSet, RoutedEventHandler checkedEventHandler, RoutedEventHandler uncheckedEventHandler) {
            Index = index;
            TagFilterSet = tagFilterSet;
            InitializeComponent();
            Checked += checkedEventHandler;
            Unchecked += uncheckedEventHandler;
        }
    }
}
