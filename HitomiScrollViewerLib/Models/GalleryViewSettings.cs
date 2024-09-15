using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;
using Windows.Storage;

namespace HitomiScrollViewerLib.Models {
    public partial class GalleryViewSettings : ObservableObject {
        public static readonly (int Min, int Max) AUTO_SCROLL_INTERVAL_RANGE = (1, 10);
        public const double AUTO_SCROLL_INTERVAL_FREQUENCY = 0.25;
        private static readonly List<ScrollDirection> SCROLL_DIRECTIONS = [
            new() { Value = Orientation.Vertical },
            new() { Value = Orientation.Horizontal }
        ];
        public static readonly List<FlowDirectionModel> FLOW_DIRECTION_MODELS = [
            new() { Value = FlowDirection.LeftToRight },
            new() { Value = FlowDirection.RightToLeft }
        ];

        [ObservableProperty]
        private double _autoScrollInterval = (double)(ApplicationData.Current.LocalSettings.Values[nameof(AutoScrollInterval)] ??= 5);
        [ObservableProperty]
        private bool _isLoopEnabled = (bool)(ApplicationData.Current.LocalSettings.Values[nameof(IsLoopEnabled)] ??= true);
        [ObservableProperty]
        private FlowDirectionModel _flowDirectionModel = FLOW_DIRECTION_MODELS.Find(fd => fd.Value == (FlowDirection)(ApplicationData.Current.LocalSettings.Values[nameof(FlowDirection)] ??= FlowDirection.RightToLeft));
        [ObservableProperty]
        private bool _isPageFlipEffectEnabled = (bool)(ApplicationData.Current.LocalSettings.Values[nameof(IsPageFlipEffectEnabled)] ??= true);
        [ObservableProperty]
        private ScrollDirection _scrollDirection = SCROLL_DIRECTIONS.Find(sd => sd.Value == (Orientation)(ApplicationData.Current.LocalSettings.Values[nameof(ScrollDirection)] ??= Orientation.Vertical));

        partial void OnAutoScrollIntervalChanged(double value) {
            ApplicationData.Current.LocalSettings.Values[nameof(AutoScrollInterval)] = value;
        }
        partial void OnIsLoopEnabledChanged(bool value) {
            ApplicationData.Current.LocalSettings.Values[nameof(IsLoopEnabled)] = value;
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
    }
}
