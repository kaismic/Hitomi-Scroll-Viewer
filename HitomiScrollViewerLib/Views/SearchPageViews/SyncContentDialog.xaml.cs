using HitomiScrollViewerLib.ViewModels.SearchPageVMs;
using Microsoft.UI.Xaml.Controls;

namespace HitomiScrollViewerLib.Views.SearchPageViews {
    public sealed partial class SyncContentDialog : ContentDialog {
        public SyncContentDialogVM ViewModel { get; set; }
        public SyncContentDialog() {
            InitializeComponent();
        }
    }
}
