using HitomiScrollViewerLib.Entities;
using HitomiScrollViewerLib.Models;
using System.Collections.ObjectModel;

namespace HitomiScrollViewerLib.ViewModels {
    public partial class PairedTFSSelectorVM(ObservableCollection<TagFilterSet> tfss) : TFSSelectorVM(tfss) {
        public PairedTFSSelectorVM OtherTFSSelectorVM { private get; set; }

        private void EnableCheckBox(int i, bool enable) {
            TfsCheckBoxModels[i].IsEnabled = enable;
        }

        public override void CheckBoxToggleHandler(TFSCheckBoxModel model) {
            base.CheckBoxToggleHandler(model);
            OtherTFSSelectorVM.EnableCheckBox(TfsCheckBoxModels.IndexOf(model), !model.IsChecked);
        }
    }
}
