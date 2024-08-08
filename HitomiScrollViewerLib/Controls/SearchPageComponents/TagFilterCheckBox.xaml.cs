using HitomiScrollViewerLib.Entities;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace HitomiScrollViewerLib.Controls.SearchPageComponents {
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
