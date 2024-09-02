using HitomiScrollViewerLib.ViewModels.SearchPageVMs;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace HitomiScrollViewerLib.Views.SearchPageViews {
    public partial class TFSSelector : Grid {
        public TFSSelectorVM ViewModel { get; set; }

        public TFSSelector() {
            InitializeComponent();
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e) {
            ViewModel.TFSCheckBox_Checked(sender, e);
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e) {
            ViewModel.TFSCheckBox_Unchecked(sender, e);
        }
    }
}
