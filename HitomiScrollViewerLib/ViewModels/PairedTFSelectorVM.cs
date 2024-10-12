using HitomiScrollViewerLib.Models;
using System;

namespace HitomiScrollViewerLib.ViewModels {
    public partial class PairedTFSelectorVM : TFSelectorVM {
        public PairedTFSelectorVM OtherTFSelectorVM { private get; set; }

        public event Action CheckBoxToggled;

        private void EnableCheckBox(int i, bool enable) {
            TFCheckBoxModels[i].IsEnabled = enable;
        }

        public override void CheckBox_Toggled(TFCheckBoxModel model) {
            base.CheckBox_Toggled(model);
            OtherTFSelectorVM.EnableCheckBox(TFCheckBoxModels.IndexOf(model), !model.IsChecked);
            CheckBoxToggled?.Invoke();
        }
    }
}
