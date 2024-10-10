using HitomiScrollViewerLib.ViewModels.SearchPageVMs;
using Microsoft.UI.Xaml.Controls;
using static HitomiScrollViewerLib.SharedResources;

namespace HitomiScrollViewerLib.Views.SearchPageViews {
    public sealed partial class SyncContentDialog : ContentDialog {

        public SyncContentDialogVM _viewModel;
        public SyncContentDialogVM ViewModel {
            get => _viewModel;
            set {
                _viewModel = value;
            }
        }

        public SyncContentDialog() {
            InitializeComponent();
            RadioButtons_4.Items.Add(new RadioButton() { Content = TEXT_YES });
            RadioButtons_4.Items.Add(new RadioButton() { Content = TEXT_NO });
        }
    }
}
