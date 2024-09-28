using CommunityToolkit.Mvvm.ComponentModel;
using HitomiScrollViewerLib.Views;
using System.ComponentModel;

namespace HitomiScrollViewerLib {
    public class DQObservableObject : ObservableObject {
        protected override void OnPropertyChanging(PropertyChangingEventArgs e) {
            MainWindow.MainDispatcherQueue.TryEnqueue(() => base.OnPropertyChanging(e));
        }
        protected override void OnPropertyChanged(PropertyChangedEventArgs e) {
            MainWindow.MainDispatcherQueue.TryEnqueue(() => base.OnPropertyChanged(e));
        }
    }
}
