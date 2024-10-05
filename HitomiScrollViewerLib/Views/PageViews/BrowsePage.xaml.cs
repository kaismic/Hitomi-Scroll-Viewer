using HitomiScrollViewerLib.ViewModels.PageVMs;
using HitomiScrollViewerLib.Views.BrowsePageViews;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
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
                }
            }
        }

        private SortDialog _sortDialog;
        private bool _isDialogOpen = false;

        public BrowsePage() {
            InitializeComponent();
            PageTextBlock.Text = TEXT_PAGE;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e) {
            base.OnNavigatedTo(e);
            ViewModel = BrowsePageVM.Main;
        }

        private void SortDialogButton_Clicked(object _0, RoutedEventArgs _1) {
            if (_isDialogOpen) {
                _sortDialog.Hide();
            } else {
                _ = _sortDialog.ShowAsync();
            }
        }
    }
}
