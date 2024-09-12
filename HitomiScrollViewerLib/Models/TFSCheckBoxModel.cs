using CommunityToolkit.Mvvm.ComponentModel;
using HitomiScrollViewerLib.Entities;
using System.Windows.Input;

namespace HitomiScrollViewerLib.Models {
    public partial class TFSCheckBoxModel(
        TagFilter tagFilter,
        ICommand checkBoxToggleCommand
    ) : ObservableObject {
        [ObservableProperty]
        private bool _isChecked;
        [ObservableProperty]
        private bool _isEnabled;
        public TagFilter TagFilter { get; } = tagFilter;
        public ICommand CheckBoxToggleCommand { get; } = checkBoxToggleCommand;
    }
}