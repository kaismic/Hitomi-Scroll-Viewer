using HitomiScrollViewerLib.ViewModels.SearchPageVMs;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace HitomiScrollViewerLib.Views.SearchPageViews {
    public sealed partial class SyncManager : Grid {
        public SyncManagerVM ViewModel { get; set; }

        public SyncManager() {
            InitializeComponent();
        }
    }
}
