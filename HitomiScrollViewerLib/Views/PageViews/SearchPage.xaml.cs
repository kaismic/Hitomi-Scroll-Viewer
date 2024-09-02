using HitomiScrollViewerLib.ViewModels.PageVMs;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Windows.Foundation;

namespace HitomiScrollViewerLib.Views.PageViews {
    public sealed partial class SearchPage : Page {
        public SearchPageVM ViewModel { get; set; }
        private Thickness PopupInfoBarStackPanelMargin = new();

        public SearchPage() {
            InitializeComponent();
            Loaded += SearchPage_Loaded;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e) {
            base.OnNavigatedTo(e);
            ViewModel = (SearchPageVM)e.Parameter;
        }

        private void SearchPage_Loaded(object _0, RoutedEventArgs _1) {
            Loaded -= SearchPage_Loaded;
            ViewModel.PageSize = new Size(ActualWidth, ActualHeight);
            PopupInfoBarStackPanelMargin.Bottom = ActualHeight / 16;
        }

        private void SearchPage_SizeChanged(object _0, SizeChangedEventArgs e) {
            ViewModel.PageSize = e.NewSize;
            PopupInfoBarStackPanelMargin.Bottom = ActualHeight / 16;
        }
    }
}
