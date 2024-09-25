﻿using HitomiScrollViewerLib.Entities;
using HitomiScrollViewerLib.Models;
using System.Collections.ObjectModel;

namespace HitomiScrollViewerLib.ViewModels {
    public partial class PairedTFSelectorVM(ObservableCollection<TagFilter> tfss) : TFSelectorVM(tfss) {
        public PairedTFSelectorVM OtherTFSSelectorVM { private get; set; }

        private void EnableCheckBox(int i, bool enable) {
            TfCheckBoxModels[i].IsEnabled = enable;
        }

        public override void CheckBoxToggleHandler(TFCheckBoxModel model) {
            base.CheckBoxToggleHandler(model);
            OtherTFSSelectorVM.EnableCheckBox(TfCheckBoxModels.IndexOf(model), !model.IsChecked);
        }
    }
}
