using HitomiScrollViewerLib.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace HitomiScrollViewerLib.Views {
    public sealed partial class CRUDContentDialog : ContentDialog {
        public CRUDContentDialogVM ViewModel { get; set; }
        public CRUDContentDialog() {
            InitializeComponent();
        }
    }
}
