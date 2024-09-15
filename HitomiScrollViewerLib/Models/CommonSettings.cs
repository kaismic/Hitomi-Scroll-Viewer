using CommunityToolkit.Mvvm.ComponentModel;
using Windows.Storage;

namespace HitomiScrollViewerLib.Models {
    public partial class CommonSettings : ObservableObject {
        [ObservableProperty]
        private bool _isTFAutoSaveEnabled = (bool)(ApplicationData.Current.LocalSettings.Values[nameof(IsTFAutoSaveEnabled)] ??= true);
        partial void OnIsTFAutoSaveEnabledChanged(bool value) {
            ApplicationData.Current.LocalSettings.Values[nameof(IsTFAutoSaveEnabled)] = value;
        }

        private static CommonSettings _main;
        public static CommonSettings Main => _main ??= new();
        private CommonSettings() { }
    }
}
