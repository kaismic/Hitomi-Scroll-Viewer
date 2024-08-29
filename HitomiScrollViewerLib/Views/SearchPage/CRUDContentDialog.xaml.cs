using HitomiScrollViewerLib.ViewModels.SearchPage;
using Microsoft.UI.Xaml.Controls;

namespace HitomiScrollViewerLib.Views.SearchPage {
    public sealed partial class CRUDContentDialog : ContentDialog {
        public CRUDContentDialogVM ViewModel { get; set; }
        public CRUDContentDialog() {
            InitializeComponent();
        }
    }
}
