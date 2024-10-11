using HitomiScrollViewerLib.ViewModels.BrowsePageVMs;
using HitomiScrollViewerLib.ViewModels.PageVMs;
using HitomiScrollViewerLib.Views.BrowsePageViews;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

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
        private ItemsWrapGrid _itemsWrapGrid;

        public double GalleryBrowseItemWidth {
            get => (double)GetValue(GalleryBrowseItemWidthProperty);
            set => SetValue(GalleryBrowseItemWidthProperty, value);
        }
        public static readonly DependencyProperty GalleryBrowseItemWidthProperty =
            DependencyProperty.Register(
                nameof(GalleryBrowseItemWidth),
                typeof(double),
                typeof(BrowsePage),
                new(0)
            );

        public BrowsePage() {
            InitializeComponent();
        }

        private void SetGalleryItemWidths() {
            if (GalleryGridView.ItemsPanelRoot == null) {
                return;
            }
            _itemsWrapGrid = GalleryGridView.ItemsPanelRoot as ItemsWrapGrid;
            if (ViewModel.CurrentGalleryBrowseItemVMs.Count > 0) {
                GalleryBrowseItemWidth = GalleryGridView.ActualWidth / _itemsWrapGrid.MaximumRowsOrColumns - 8;
            }
        }

        private const double MIN_GALLERY_ITEM_WIDTH = 800;
        private void GalleryGridView_SizeChanged(object _0, SizeChangedEventArgs e) {
            _itemsWrapGrid = GalleryGridView.ItemsPanelRoot as ItemsWrapGrid;
            if (e.NewSize.Width < MIN_GALLERY_ITEM_WIDTH) {
                _itemsWrapGrid.MaximumRowsOrColumns = 1;
            } else {
                _itemsWrapGrid.MaximumRowsOrColumns = (int)(e.NewSize.Width / MIN_GALLERY_ITEM_WIDTH);
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

        private void GalleryBrowseItem_RightTapped(object sender, RightTappedRoutedEventArgs _1) {
            GalleryBrowseItemVM vm = (sender as GalleryBrowseItem).ViewModel;
            if (!GalleryGridView.SelectedItems.Contains(vm)) {
                GalleryGridView.SelectedItems.Clear();
                GalleryGridView.SelectedItems.Add(vm);
            }
        }

        private void GalleryGridView_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            ViewModel.SelectedGalleryBrowseItemVMs = [.. GalleryGridView.SelectedItems.Cast<GalleryBrowseItemVM>()];
        }
    }
}
