using HitomiScrollViewerLib.ViewModels.SearchPage;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace HitomiScrollViewerLib.Views.SearchPage {
    public partial class TFSSelector : Grid {
        public TFSSelectorVM ViewModel => (TFSSelectorVM)DataContext;
        public TFSSelector(TFSSelectorVM viewModel) {
            InitializeComponent();
            DataContext = viewModel;
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e) {
            ViewModel.TFSCheckBox_Checked(sender, e);
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e) {
            ViewModel.TFSCheckBox_Unchecked(sender, e);
        }
    }
}
