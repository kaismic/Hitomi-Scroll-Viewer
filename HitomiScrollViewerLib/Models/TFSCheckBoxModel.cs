using CommunityToolkit.Mvvm.ComponentModel;
using HitomiScrollViewerLib.Entities;
using HitomiScrollViewerLib.Views;
using System.Windows.Input;

namespace HitomiScrollViewerLib.Models {
    public partial class TFCheckBoxModel(
        TagFilter tagFilter,
        ICommand checkBoxToggleCommand
    ) : ObservableObject {
        [ObservableProperty]
        private bool _isChecked;
        private bool _isEnabled;
        public bool IsEnabled {
            get => _isEnabled;
            set {
                MainWindow.MainDispatcherQueue.TryEnqueue(() => {
                    SetProperty(ref _isEnabled, value);
                });
            }
        }

        public TagFilter TagFilter { get; } = tagFilter;
        public ICommand CheckBoxToggleCommand { get; } = checkBoxToggleCommand;
    }
}