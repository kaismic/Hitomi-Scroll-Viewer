using HitomiScrollViewerLib.Controls.SearchPageComponents;
using HitomiScrollViewerLib.Entities;
using Microsoft.UI.Xaml;
using System.Collections.ObjectModel;

namespace HitomiScrollViewerLib.ViewModels.SearchPageVMs {
    public partial class PairedTFSSelectorVM(ObservableCollection<TagFilterSet> tfss) : TFSSelectorVM(tfss) {
        public PairedTFSSelectorVM OtherTFSSelectorVM { private get; set; }

        private void EnableCheckBox(int i, bool enable) {
            TfsCheckBoxes[i].IsEnabled = enable;
        }

        public override void TFSCheckBox_Checked(object sender, RoutedEventArgs e) {
            base.TFSCheckBox_Checked(sender, e);
            OtherTFSSelectorVM.EnableCheckBox(TfsCheckBoxes.IndexOf((TFSCheckBox)sender), false);
        }

        public override void TFSCheckBox_Unchecked(object sender, RoutedEventArgs e) {
            base.TFSCheckBox_Unchecked(sender, e);
            OtherTFSSelectorVM.EnableCheckBox(TfsCheckBoxes.IndexOf((TFSCheckBox)sender), true);
        }
    }
}
