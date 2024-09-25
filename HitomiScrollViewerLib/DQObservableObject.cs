using CommunityToolkit.Mvvm.ComponentModel;
using HitomiScrollViewerLib.Views;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

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
