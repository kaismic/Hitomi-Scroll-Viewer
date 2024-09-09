using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Controls;
using System.Windows.Input;

namespace HitomiScrollViewerLib.Models {
    public partial class InfoBarModel : ObservableObject {
        [ObservableProperty]
        private bool _isOpen;
        [ObservableProperty]
        private InfoBarSeverity _severity;
        [ObservableProperty]
        private string _title;
        [ObservableProperty]
        private string _message;
        [ObservableProperty]
        private ICommand _closeButtonCommand;
    }
}
