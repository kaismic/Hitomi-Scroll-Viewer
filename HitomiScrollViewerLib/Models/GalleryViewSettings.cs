using CommunityToolkit.Mvvm.ComponentModel;
using Windows.Storage;

namespace HitomiScrollViewerLib.Models {
    public partial class GalleryViewSettings : ObservableObject {
        public static readonly (int Min, int Max) AUTO_SCROLL_INTERVAL_RANGE = (1, 10);
        public const double AUTO_SCROLL_INTERVAL_FREQUENCY = 0.25;

        [ObservableProperty]
        private double _autoScrollInterval = (double)(ApplicationData.Current.LocalSettings.Values[nameof(AutoScrollInterval)] ??= 5);
        [ObservableProperty]
        private bool _isLoopEnabled = (bool)(ApplicationData.Current.LocalSettings.Values[nameof(IsLoopEnabled)] ??= true);

        partial void OnAutoScrollIntervalChanged(double value) {
            ApplicationData.Current.LocalSettings.Values[nameof(AutoScrollInterval)] = value;
        }
        partial void OnIsLoopEnabledChanged(bool value) {
            ApplicationData.Current.LocalSettings.Values[nameof(IsLoopEnabled)] = value;
        }
    }
}
