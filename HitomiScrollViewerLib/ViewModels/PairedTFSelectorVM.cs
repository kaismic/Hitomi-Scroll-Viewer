using HitomiScrollViewerLib.Entities;
using HitomiScrollViewerLib.Models;
using System;
using System.Collections.ObjectModel;

namespace HitomiScrollViewerLib.ViewModels {
    public partial class PairedTFSelectorVM(ObservableCollection<TagFilter> tf) : TFSelectorVM(tf) {
        public PairedTFSelectorVM OtherTFSelectorVM { private get; set; }

        public event Action CheckBoxToggled;

        private void EnableCheckBox(int i, bool enable) {
            TfCheckBoxModels[i].IsEnabled = enable;
        }

        public override void CheckBox_Toggled(TFCheckBoxModel model) {
            base.CheckBox_Toggled(model);
            OtherTFSelectorVM.EnableCheckBox(TfCheckBoxModels.IndexOf(model), !model.IsChecked);
            CheckBoxToggled?.Invoke();
        }
    }
}
