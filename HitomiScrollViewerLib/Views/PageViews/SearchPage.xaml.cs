using HitomiScrollViewerLib.ViewModels.PageVMs;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace HitomiScrollViewerLib.Views.PageViews {
    public sealed partial class SearchPage : Page {
        private SearchPageVM ViewModel { get; set; }

        public SearchPage() {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e) {
            base.OnNavigatedTo(e);
            ViewModel = SearchPageVM.Main;
        }
    }
}
