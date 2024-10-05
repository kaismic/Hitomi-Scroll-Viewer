using HitomiScrollViewerLib.ViewModels.PageVMs;
using HitomiScrollViewerLib.Views.BrowsePageViews;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System.ComponentModel;
using static HitomiScrollViewerLib.SharedResources;

namespace HitomiScrollViewerLib.Views.PageViews {
    public sealed partial class BrowsePage : Page {
        private BrowsePageVM _viewModel;
        private BrowsePageVM ViewModel {
            get => _viewModel;
            set {
                if (_viewModel == null) {
                    _viewModel = value;
                    _sortDialog = new(value.SortDialogVM);
                    value.CurrentGalleryBrowseItemsChanged += SetGalleryBrowseItemWidths;
                }
            }
        }

        private SortDialog _sortDialog;
        private bool _isDialogOpen = false;

        public BrowsePage() {
            InitializeComponent();
            PageTextBlock.Text = TEXT_PAGE;

            GalleryBrowseItemsView.SizeChanged += GalleryBrowseItemsView_SizeChanged;

        }

        private void SetGalleryBrowseItemWidths() {
            if (GalleryBrowseItemsView.ActualWidth != 0) {
                foreach (var vm in ViewModel.CurrentGalleryBrowseItemVMs) {
                    vm.Width = (GalleryBrowseItemsView.ActualWidth - GalleryBrowseItemsViewLayout.MinColumnSpacing * (GalleryBrowseItemsViewLayout.MaximumRowsOrColumns - 1)) / GalleryBrowseItemsViewLayout.MaximumRowsOrColumns;
                }
            }
        }

        private void GalleryBrowseItemsView_SizeChanged(object sender, SizeChangedEventArgs e) {
            SetGalleryBrowseItemWidths();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e) {
            base.OnNavigatedTo(e);
            ViewModel = BrowsePageVM.Main;
        }

        private void SortDialogButton_Clicked(object _0, RoutedEventArgs _1) {
            if (_isDialogOpen) {
                _sortDialog.Hide();
            } else {
                _sortDialog.XamlRoot = XamlRoot;
                _ = _sortDialog.ShowAsync();
            }
            _isDialogOpen = !_isDialogOpen;
        }
    }
}
