using HitomiScrollViewerLib.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace HitomiScrollViewerLib.Views {
    public sealed partial class SearchFilterView : StackPanel {
        public SearchFilterVM ViewModel { get; set; }
        public SearchFilterView() {
            InitializeComponent();
        }
    }
}
