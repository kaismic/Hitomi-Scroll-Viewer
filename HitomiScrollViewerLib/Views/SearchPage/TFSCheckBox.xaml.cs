using HitomiScrollViewerLib.Entities;
using Microsoft.UI.Xaml.Controls;

namespace HitomiScrollViewerLib.Controls.SearchPageComponents {
    public sealed partial class TFSCheckBox : CheckBox {
        public TagFilterSet TagFilterSet { get; private set; }

        public TFSCheckBox(TagFilterSet tagFilterSet) {
            TagFilterSet = tagFilterSet;
            InitializeComponent();
        }
    }
}