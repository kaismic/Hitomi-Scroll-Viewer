using HitomiScrollViewerLib.ViewModels.PageVMs;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace HitomiScrollViewerLib.Views.PageViews {
    public sealed partial class ViewPage : Page {
        public ViewPageVM ViewModel { get; set; }

        public ViewPage() {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e) {
            base.OnNavigatedTo(e);
            ViewModel = ViewPageVM.Main;
        }
    }
}
