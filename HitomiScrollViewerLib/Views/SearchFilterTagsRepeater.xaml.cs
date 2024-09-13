using HitomiScrollViewerLib.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace HitomiScrollViewerLib.Views {
    public sealed partial class SearchFilterTagsRepeater : Grid {
        public SearchFilterTagsRepeaterVM ViewModel { get; set; }
        public SearchFilterTagsRepeater() {
            InitializeComponent();
        }
    }
}
