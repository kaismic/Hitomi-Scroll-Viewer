using HitomiScrollViewerLib.ViewModels.PageVMs;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace HitomiScrollViewerLib.Views.PageViews {
    public sealed partial class BrowsePage : Page {
        public BrowsePageVM ViewModel { get; set; }
        public BrowsePage() {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e) {
            base.OnNavigatedTo(e);
            ViewModel = (BrowsePageVM)e.Parameter;
        }
    }
}
