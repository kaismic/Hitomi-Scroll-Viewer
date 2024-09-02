using HitomiScrollViewerLib.ViewModels.SearchPageVMs;
using Microsoft.UI.Xaml.Controls;

namespace HitomiScrollViewerLib.Views.SearchPageViews {
    public sealed partial class DownloadItem : Grid {
        public DownloadItemVM ViewModel { get; set; }

        public DownloadItem() {
            InitializeComponent();
        }
    }
}
