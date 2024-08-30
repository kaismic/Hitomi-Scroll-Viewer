using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Controls;

namespace HitomiScrollViewerLib.ViewModels.SearchPage.SyncContentDialogVMs {
    public partial class InfoBarVM : ObservableObject {
        [ObservableProperty]
        private bool _isOpen;
        [ObservableProperty]
        private InfoBarSeverity _severity;
        [ObservableProperty]
        private string _title;
        [ObservableProperty]
        private string _message;
    }
}
