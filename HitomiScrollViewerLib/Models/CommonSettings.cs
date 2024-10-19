using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;
using System.Linq;
using Windows.Storage;

namespace HitomiScrollViewerLib.Models {
    public partial class CommonSettings : DQObservableObject {
        public static readonly List<FlowDirectionModel> FLOW_DIRECTION_MODELS = [
            new() { Value = FlowDirection.LeftToRight },
            new() { Value = FlowDirection.RightToLeft }
        ];
        public static readonly List<ScrollDirection> SCROLL_DIRECTIONS = [
            new() { Value = Orientation.Vertical },
            new() { Value = Orientation.Horizontal }
        ];
        public static readonly List<string> IMAGES_PER_PAGE_ITEMS = [
            "Auto (Recommended)",
            .. Enumerable.Range(1, 5).Select(x => x.ToString())
        ];

        [ObservableProperty]
        private FlowDirectionModel _flowDirectionModel = FLOW_DIRECTION_MODELS.Find(fd => fd.Value == (FlowDirection)(ApplicationData.Current.LocalSettings.Values[nameof(FlowDirection)] ??= (int)FlowDirection.RightToLeft));
        partial void OnFlowDirectionModelChanged(FlowDirectionModel value) {
            ApplicationData.Current.LocalSettings.Values[nameof(FlowDirectionModel)] = (int)value.Value;
        }

        [ObservableProperty]
        private bool _isPageFlipEffectEnabled = (bool)(ApplicationData.Current.LocalSettings.Values[nameof(IsPageFlipEffectEnabled)] ??= true);
        partial void OnIsPageFlipEffectEnabledChanged(bool value) {
            ApplicationData.Current.LocalSettings.Values[nameof(IsPageFlipEffectEnabled)] = value;
        }

        [ObservableProperty]
        private ScrollDirection _scrollDirection = SCROLL_DIRECTIONS.Find(sd => sd.Value == (Orientation)(ApplicationData.Current.LocalSettings.Values[nameof(ScrollDirection)] ??= (int)Orientation.Vertical));
        partial void OnScrollDirectionChanged(ScrollDirection value) {
            ApplicationData.Current.LocalSettings.Values[nameof(ScrollDirection)] = (int)value.Value;
        }

        [ObservableProperty]
        private int _imagesPerPage = (int)(ApplicationData.Current.LocalSettings.Values[nameof(ImagesPerPage)] ??= 0);
        partial void OnImagesPerPageChanged(int value) {
            ApplicationData.Current.LocalSettings.Values[nameof(ImagesPerPage)] = value;
        }
        public static CommonSettings Main { get; } = new();
        private CommonSettings() {}
    }
}
