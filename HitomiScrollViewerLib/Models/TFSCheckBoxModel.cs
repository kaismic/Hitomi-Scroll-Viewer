using CommunityToolkit.Mvvm.ComponentModel;
using HitomiScrollViewerLib.Entities;
using System.Windows.Input;

namespace HitomiScrollViewerLib.Models {
    public partial class TFCheckBoxModel(TagFilter tagFilter, ICommand checkBoxToggleCommand) : DQObservableObject {
        [ObservableProperty]
        private bool _isChecked;
        [ObservableProperty]
        private bool _isEnabled;
        public TagFilter TagFilter { get; } = tagFilter;
        public ICommand CheckBoxToggleCommand { get; } = checkBoxToggleCommand;
    }
}