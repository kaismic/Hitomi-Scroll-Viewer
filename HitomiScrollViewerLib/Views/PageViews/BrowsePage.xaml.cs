using HitomiScrollViewerLib.ViewModels.PageVMs;
using HitomiScrollViewerLib.Views.BrowsePageViews;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
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
                    value.CurrentGalleryBrowseItemsChanged += SetGalleryItemWidths;
                }
            }
        }

        private SortDialog _sortDialog;

        public BrowsePage() {
            InitializeComponent();
        }

        private void SetGalleryItemWidths() {
            if (ViewModel.CurrentGalleryBrowseItemVMs.Count > 0) {
                foreach (var vm in ViewModel.CurrentGalleryBrowseItemVMs) {
                    vm.Width =
                        (
                        GalleryItemsView.ActualWidth
                        - GalleryItemsViewLayout.MinColumnSpacing * (GalleryItemsViewLayout.MaximumRowsOrColumns - 1)
                        )
                        / GalleryItemsViewLayout.MaximumRowsOrColumns - 8;
                }
            }
        }

        private const double MIN_GALLERY_ITEM_WIDTH = 800;
        private void GalleryItemsView_SizeChanged(object _0, SizeChangedEventArgs e) {
            if (e.NewSize.Width < MIN_GALLERY_ITEM_WIDTH) {
                GalleryItemsViewLayout.MaximumRowsOrColumns = 1;
            } else {
                GalleryItemsViewLayout.MaximumRowsOrColumns = (int)Math.Floor(e.NewSize.Width / MIN_GALLERY_ITEM_WIDTH);
            }
            SetGalleryItemWidths();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e) {
            base.OnNavigatedTo(e);
            ViewModel = BrowsePageVM.Main;
        }

        private void SortDialogButton_Clicked(object _0, RoutedEventArgs _1) {
            _sortDialog.XamlRoot = XamlRoot;
            _ = _sortDialog.ShowAsync();
        }
    }
}
