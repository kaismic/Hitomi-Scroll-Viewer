using HitomiScrollViewerLib.ViewModels.BrowsePageVMs;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace HitomiScrollViewerLib.Views.BrowsePageViews {
    public sealed partial class CandidateSortItemView : Grid {
        public SortItemVM _viewModel;
        public SortItemVM ViewModel {
            get => _viewModel;
            set {
                if (_viewModel != null) {
                    return;
                }
                _viewModel = value;
                SortItemTextBlock.Text = value.GallerySort.DisplayName;
            }
        }
        public CandidateSortItemView() {
            InitializeComponent();
            for (int i = 0; i < Children.Count; i++) {
                SetColumn(Children[i] as FrameworkElement, i);
            }
        }
    }
}
