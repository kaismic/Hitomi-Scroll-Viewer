using System.ComponentModel;
using Windows.Storage;

namespace HitomiScrollViewerLib.Models {
    public partial class GalleryViewSettings : INotifyPropertyChanged {
        public event PropertyChangedEventHandler PropertyChanged;

        public const int AUTO_SCROLL_INTERVAL_MIN_VALUE = 1;
        public const int AUTO_SCROLL_INTERVAL_MAX_VALUE = 10;
        public const double AUTO_SCROLL_INTERVAL_FREQUENCY = 0.25;

        private double _autoScrollInterval = (double)(ApplicationData.Current.LocalSettings.Values[nameof(AutoScrollInterval)] ??= 5);
        public double AutoScrollInterval {
            get => _autoScrollInterval;
            set {
                _autoScrollInterval = value;
                ApplicationData.Current.LocalSettings.Values[nameof(AutoScrollInterval)] = value;
            }
        }

        private bool _isLoopEnabled = (bool)(ApplicationData.Current.LocalSettings.Values[nameof(IsLoopEnabled)] ??= true);

        public bool IsLoopEnabled {
            get => _isLoopEnabled;
            set {
                _isLoopEnabled = value;
                ApplicationData.Current.LocalSettings.Values[nameof(IsLoopEnabled)] = value;
                PropertyChanged?.Invoke(this, new(nameof(IsLoopEnabled)));
            }
        }
    }
}
