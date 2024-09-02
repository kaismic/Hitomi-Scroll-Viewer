using HitomiScrollViewerLib.ViewModels.SearchPageVMs;
using Microsoft.UI.Xaml.Controls;

namespace HitomiScrollViewerLib.Views.SearchPageViews {
    public sealed partial class CRUDContentDialog : ContentDialog {
        public CRUDContentDialogVM ViewModel { get; set; }
        public CRUDContentDialog() {
            InitializeComponent();
        }
    }
}
