using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;
using Windows.Storage;

namespace HitomiScrollViewerLib.Models {
    public partial class CommonSettings : ObservableObject {
        public static readonly List<FlowDirectionModel> FLOW_DIRECTION_MODELS = [
            new() { Value = FlowDirection.LeftToRight },
            new() { Value = FlowDirection.RightToLeft }
        ];
        public static readonly List<ScrollDirection> SCROLL_DIRECTIONS = [
            new() { Value = Orientation.Vertical },
            new() { Value = Orientation.Horizontal }
        ];

        [ObservableProperty]
        private bool _isTFAutoSaveEnabled = (bool)(ApplicationData.Current.LocalSettings.Values[nameof(IsTFAutoSaveEnabled)] ??= true);
        [ObservableProperty]
        private FlowDirectionModel _flowDirectionModel = FLOW_DIRECTION_MODELS.Find(fd => fd.Value == (FlowDirection)(ApplicationData.Current.LocalSettings.Values[nameof(FlowDirection)] ??= FlowDirection.RightToLeft));
        [ObservableProperty]
        private bool _isPageFlipEffectEnabled = (bool)(ApplicationData.Current.LocalSettings.Values[nameof(IsPageFlipEffectEnabled)] ??= true);
        [ObservableProperty]
        private ScrollDirection _scrollDirection = SCROLL_DIRECTIONS.Find(sd => sd.Value == (Orientation)(ApplicationData.Current.LocalSettings.Values[nameof(ScrollDirection)] ??= Orientation.Vertical));

        partial void OnIsTFAutoSaveEnabledChanged(bool value) {
            ApplicationData.Current.LocalSettings.Values[nameof(IsTFAutoSaveEnabled)] = value;
        }
        partial void OnFlowDirectionModelChanged(FlowDirectionModel value) {
            ApplicationData.Current.LocalSettings.Values[nameof(FlowDirectionModel)] = value.Value;
        }
        partial void OnIsPageFlipEffectEnabledChanged(bool value) {
            ApplicationData.Current.LocalSettings.Values[nameof(IsPageFlipEffectEnabled)] = value;
        }
        partial void OnScrollDirectionChanged(ScrollDirection value) {
            ApplicationData.Current.LocalSettings.Values[nameof(ScrollDirection)] = value.Value;
        }

        private static CommonSettings _main;
        public static CommonSettings Main => _main ??= new();
        private CommonSettings() { }
    }
}
