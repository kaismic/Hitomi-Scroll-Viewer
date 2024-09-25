using CommunityToolkit.Mvvm.ComponentModel;
using Windows.Storage;

namespace HitomiScrollViewerLib.Models {
    public partial class GalleryViewSettings : DQObservableObject {
        public const int AUTO_SCROLL_INTERVAL_MIN_VALUE = 1;
        public const int AUTO_SCROLL_INTERVAL_MAX_VALUE = 10;
        public const double AUTO_SCROLL_INTERVAL_FREQUENCY = 0.25;

        [ObservableProperty]
        private double _autoScrollInterval = (double)(ApplicationData.Current.LocalSettings.Values[nameof(AutoScrollInterval)] ??= 5);
        partial void OnAutoScrollIntervalChanged(double value) {
            ApplicationData.Current.LocalSettings.Values[nameof(AutoScrollInterval)] = value;
        }

        [ObservableProperty]
        private bool _isLoopEnabled = (bool)(ApplicationData.Current.LocalSettings.Values[nameof(IsLoopEnabled)] ??= true);
        partial void OnIsLoopEnabledChanged(bool value) {
            ApplicationData.Current.LocalSettings.Values[nameof(IsLoopEnabled)] = value;
        }
    }
}
