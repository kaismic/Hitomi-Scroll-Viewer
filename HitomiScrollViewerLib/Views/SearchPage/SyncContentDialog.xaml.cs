using HitomiScrollViewerLib.ViewModels.SearchPage;
using Microsoft.UI.Xaml.Controls;
using static HitomiScrollViewerLib.SharedResources;

namespace HitomiScrollViewerLib.Views.SearchPage {
    public sealed partial class SyncContentDialog : ContentDialog {
        public SyncContentDialogVM ViewModel { get; set; }
        public SyncContentDialog(SyncContentDialogVM viewModel) {
            ViewModel = viewModel;
            InitializeComponent();
            RadioButtons_4.Items.Add(new RadioButton() { Content = TEXT_YES });
            RadioButtons_4.Items.Add(new RadioButton() { Content = TEXT_NO });
        }
    }
}
