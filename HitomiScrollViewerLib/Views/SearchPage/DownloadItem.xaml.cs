using HitomiScrollViewerLib.ViewModels.SearchPage;
using Microsoft.UI.Xaml.Controls;

namespace HitomiScrollViewerLib.Views.SearchPage {
    public sealed partial class DownloadItem : Grid {
        public DownloadItemVM ViewModel { get; set; }

        public DownloadItem(DownloadItemVM viewModel) {
            InitializeComponent();
            ViewModel = viewModel;
        }
    }
}
