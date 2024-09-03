using CommunityToolkit.Mvvm.ComponentModel;
using HitomiScrollViewerLib.Entities;
using System.Windows.Input;

namespace HitomiScrollViewerLib.Models.SearchPageModels {
    public partial class TFSCheckBoxModel(
        TagFilterSet tagFilterSet,
        ICommand checkBoxToggleCommand
    ) : ObservableObject {
        [ObservableProperty]
        private bool _isChecked;
        [ObservableProperty]
        private bool _isEnabled;
        public TagFilterSet TagFilterSet { get; } = tagFilterSet;
        public ICommand CheckBoxToggleCommand { get; } = checkBoxToggleCommand;
    }
}