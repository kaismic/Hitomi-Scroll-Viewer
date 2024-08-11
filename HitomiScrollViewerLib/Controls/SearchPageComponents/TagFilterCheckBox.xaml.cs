using HitomiScrollViewerLib.Entities;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace HitomiScrollViewerLib.Controls.SearchPageComponents {
    public sealed partial class TagFilterCheckBox : CheckBox {
        internal TagFilterSet TagFilterSet { get; private set; }
        public TagFilterCheckBox(TagFilterSet tagFilterSet, RoutedEventHandler checkedEventHandler, RoutedEventHandler uncheckedEventHandler) {
            TagFilterSet = tagFilterSet;
            InitializeComponent();
            Checked += checkedEventHandler;
            Unchecked += uncheckedEventHandler;
        }
    }
}
