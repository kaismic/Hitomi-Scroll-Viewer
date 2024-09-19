using HitomiScrollViewerLib.ViewModels.PageVMs;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using static HitomiScrollViewerLib.SharedResources;

namespace HitomiScrollViewerLib.Views.PageViews {
    public sealed partial class BrowsePage : Page {
        private BrowsePageVM ViewModel { get; set; }
        public BrowsePage() {
            InitializeComponent();
            PageTextBlock.Text = TEXT_PAGE;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e) {
            base.OnNavigatedTo(e);
            ViewModel = BrowsePageVM.Main;
        }
    }
}
