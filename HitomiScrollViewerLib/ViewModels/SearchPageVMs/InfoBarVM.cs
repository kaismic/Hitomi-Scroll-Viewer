using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System.Windows.Input;

namespace HitomiScrollViewerLib.ViewModels.SearchPageVMs {
    public partial class InfoBarVM : ObservableObject {
        [ObservableProperty]
        private bool _isOpen;
        [ObservableProperty]
        private InfoBarSeverity _severity;
        [ObservableProperty]
        private string _title;
        [ObservableProperty]
        private string _message;
        [ObservableProperty]
        private double _width;
        [ObservableProperty]
        private ICommand _closeButtonCommand;
    }
}
