using CommunityToolkit.Mvvm.ComponentModel;
using HitomiScrollViewerLib.Views;
using Microsoft.UI.Xaml.Controls;
using System.Windows.Input;

namespace HitomiScrollViewerLib.Models {
    public partial class InfoBarModel : ObservableObject {
        private bool _isOpen;
        public bool IsOpen {
            get => _isOpen;
            set {
                MainWindow.MainDispatcherQueue.TryEnqueue(() => {
                    SetProperty(ref _isOpen, value);
                });
            }
        }
        private InfoBarSeverity _severity;
        public InfoBarSeverity Severity {
            get => _severity;
            set {
                MainWindow.MainDispatcherQueue.TryEnqueue(() => {
                    SetProperty(ref _severity, value);
                });
            }
        }
        private string _title;
        public string Title {
            get => _title;
            set {
                MainWindow.MainDispatcherQueue.TryEnqueue(() => {
                    SetProperty(ref _title, value);
                });
            }
        }
        private string _message;
        public string Message {
            get => _message;
            set {
                MainWindow.MainDispatcherQueue.TryEnqueue(() => {
                    SetProperty(ref _message, value);
                });
            }
        }
        public ICommand CloseButtonCommand { get; set; }
    }
}
