using CommunityToolkit.Mvvm.ComponentModel;
using HitomiScrollViewerLib.Views;
using Windows.Storage;

namespace HitomiScrollViewerLib.Models {
    public partial class GalleryViewSettings : ObservableObject {
        public const int AUTO_SCROLL_INTERVAL_MIN_VALUE = 1;
        public const int AUTO_SCROLL_INTERVAL_MAX_VALUE = 10;
        public const double AUTO_SCROLL_INTERVAL_FREQUENCY = 0.25;

        private double _autoScrollInterval = (double)(ApplicationData.Current.LocalSettings.Values[nameof(AutoScrollInterval)] ??= 5);
        public double AutoScrollInterval {
            get => _autoScrollInterval;
            set {
                MainWindow.MainDispatcherQueue.TryEnqueue(() => {
                    if (SetProperty(ref _autoScrollInterval, value)) {
                        ApplicationData.Current.LocalSettings.Values[nameof(AutoScrollInterval)] = value;
                    }
                });
            }
        }

        private bool _isLoopEnabled = (bool)(ApplicationData.Current.LocalSettings.Values[nameof(IsLoopEnabled)] ??= true);
        public bool IsLoopEnabled {
            get => _isLoopEnabled;
            set {
                MainWindow.MainDispatcherQueue.TryEnqueue(() => {
                    if (SetProperty(ref _isLoopEnabled, value)) {
                        ApplicationData.Current.LocalSettings.Values[nameof(IsLoopEnabled)] = value;
                    }
                });
            }
        }
    }
}
