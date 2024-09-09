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
    }
}
