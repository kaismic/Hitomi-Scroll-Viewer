using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;
using Windows.Storage;

namespace HitomiScrollViewerLib.Models {
    public partial class ViewSettingsModel : ObservableObject {
        private const string AUTO_SCROLL_INTERVAL_SETTING_KEY = "AutoScrollInterval";
        private const string LOOP_ENABLED_SETTING_KEY = "LoopEnabled";
        private const string SCROLL_DIRECTION_SETTING_KEY = "ScrollDirection";
        private const string FLOW_DIRECTION_SETTING_KEY = "FlowDirection";
        private const string PAGE_FLIP_EFFECT_ENABLED_SETTING_KEY = "PageFlipEffectEnabled";

        public static readonly (int Min, int Max) AUTO_SCROLL_INTERVAL_RANGE = (1, 10);
        public const double AUTO_SCROLL_INTERVAL_FREQUENCY = 0.25;

        public List<ScrollDirection> ScrollDirections { get; } = [
            new() { Value = Orientation.Vertical },
            new() { Value = Orientation.Horizontal }
        ];
        public List<FlowDirectionModel> FlowDirectionModels { get; } = [
            new() { Value = FlowDirection.LeftToRight },
            new() { Value = FlowDirection.RightToLeft }
        ];

        private static ViewSettingsModel _main;
        public static ViewSettingsModel Main => _main ??= new();

        [ObservableProperty]
        private double _autoScrollInterval = (double)(ApplicationData.Current.LocalSettings.Values[AUTO_SCROLL_INTERVAL_SETTING_KEY] ??= 5);
        [ObservableProperty]
        private bool _loopEnabled = (bool)(ApplicationData.Current.LocalSettings.Values[LOOP_ENABLED_SETTING_KEY] ??= true);
        [ObservableProperty]
        private ScrollDirection _scrollDirection;
        [ObservableProperty]
        private FlowDirectionModel _flowDirectionModel;
        [ObservableProperty]
        private bool _pageFlipEffectEnabled = (bool)(ApplicationData.Current.LocalSettings.Values[PAGE_FLIP_EFFECT_ENABLED_SETTING_KEY] ??= true);

        partial void OnAutoScrollIntervalChanged(double value) {
            ApplicationData.Current.LocalSettings.Values[AUTO_SCROLL_INTERVAL_SETTING_KEY] = value;
        }
        partial void OnLoopEnabledChanged(bool value) {
            ApplicationData.Current.LocalSettings.Values[LOOP_ENABLED_SETTING_KEY] = value;
        }
        partial void OnScrollDirectionChanged(ScrollDirection value) {
            ApplicationData.Current.LocalSettings.Values[SCROLL_DIRECTION_SETTING_KEY] = value.Value;
        }
        partial void OnFlowDirectionModelChanged(FlowDirectionModel value) {
            ApplicationData.Current.LocalSettings.Values[FLOW_DIRECTION_SETTING_KEY] = value.Value;
        }
        partial void OnPageFlipEffectEnabledChanged(bool value) {
            ApplicationData.Current.LocalSettings.Values[PAGE_FLIP_EFFECT_ENABLED_SETTING_KEY] = value;
        }

        private ViewSettingsModel() {
            ScrollDirection = ScrollDirections.Find(sd => sd.Value == (Orientation)(ApplicationData.Current.LocalSettings.Values[SCROLL_DIRECTION_SETTING_KEY] ??= Orientation.Vertical));
            FlowDirectionModel = FlowDirectionModels.Find(fd => fd.Value == (FlowDirection)(ApplicationData.Current.LocalSettings.Values[FLOW_DIRECTION_SETTING_KEY] ??= FlowDirection.RightToLeft));
        }
    }
}
