using HitomiScrollViewerLib.ViewModels.BrowsePageVMs;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace HitomiScrollViewerLib.Views.BrowsePageViews {
    public sealed partial class SortItemView : Grid {
        private SortItemVM _viewModel;
        public SortItemVM ViewModel {
            get => _viewModel;
            set {
                if (_viewModel != null) {
                    return;
                }
                _viewModel = value;
                SortItemDisplayTextBlock.Text = value.GallerySort.DisplayName;
                SortDirectionComboBox.SelectedItem = value.GallerySort.SortDirectionEntity;
            }
        }
        public SortItemView() {
            InitializeComponent();
            for (int i = 0; i < Children.Count; i++) {
                SetColumn(Children[i] as FrameworkElement, i);
            }
        }
    }
}
