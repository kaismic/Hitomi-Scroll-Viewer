using Microsoft.UI.Xaml;

namespace HitomiScrollViewerLib.Controls.SearchPageComponents {
    internal class PairedTagFilterSetSelector: TagFilterSetSelector {
        internal PairedTagFilterSetSelector PairTagFilterSelector { get; set; }

        internal void EnableCheckBox(int i, bool enable) {
            _tagFilterCheckBoxes[i].IsEnabled = enable;
        }

        override protected void CheckBox_Checked(object sender, RoutedEventArgs e) {
            base.CheckBox_Checked(sender, e);
            PairTagFilterSelector.EnableCheckBox(_tagFilterCheckBoxes.IndexOf((TagFilterCheckBox)sender), false);
        }

        override protected void CheckBox_Unchecked(object sender, RoutedEventArgs e) {
            base.CheckBox_Unchecked(sender, e);
            PairTagFilterSelector.EnableCheckBox(_tagFilterCheckBoxes.IndexOf((TagFilterCheckBox)sender), true);
        }
    }
}
