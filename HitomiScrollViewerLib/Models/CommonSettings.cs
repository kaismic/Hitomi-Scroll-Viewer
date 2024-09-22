using CommunityToolkit.Mvvm.ComponentModel;
using HitomiScrollViewerLib.Views;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;
using System.Linq;
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
        public static readonly List<string> IMAGES_PER_PAGE_ITEMS = [
            "Auto (Recommended)",
            .. Enumerable.Range(1, 5).Select(x => x.ToString())
        ];

        private bool _isTFAutoSaveEnabled = (bool)(ApplicationData.Current.LocalSettings.Values[nameof(IsTFAutoSaveEnabled)] ??= true);
        public bool IsTFAutoSaveEnabled {
            get => _isTFAutoSaveEnabled;
            set {
                MainWindow.MainDispatcherQueue.TryEnqueue(() => {
                    if (SetProperty(ref _isTFAutoSaveEnabled, value)) {
                        ApplicationData.Current.LocalSettings.Values[nameof(IsTFAutoSaveEnabled)] = value;
                    }
                });
            }
        }
        
        private FlowDirectionModel _flowDirectionModel = FLOW_DIRECTION_MODELS.Find(fd => fd.Value == (FlowDirection)(ApplicationData.Current.LocalSettings.Values[nameof(FlowDirection)] ??= FlowDirection.RightToLeft));
        public FlowDirectionModel FlowDirectionModel {
            get => _flowDirectionModel;
            set {
                MainWindow.MainDispatcherQueue.TryEnqueue(() => {
                    if (SetProperty(ref _flowDirectionModel, value)) {
                        ApplicationData.Current.LocalSettings.Values[nameof(FlowDirectionModel)] = value.Value;
                    }
                });
            }
        }
        private bool _isPageFlipEffectEnabled = (bool)(ApplicationData.Current.LocalSettings.Values[nameof(IsPageFlipEffectEnabled)] ??= true);
        public bool IsPageFlipEffectEnabled {
            get => _isPageFlipEffectEnabled;
            set {
                MainWindow.MainDispatcherQueue.TryEnqueue(() => {
                    if (SetProperty(ref _isPageFlipEffectEnabled, value)) {
                        ApplicationData.Current.LocalSettings.Values[nameof(IsPageFlipEffectEnabled)] = value;
                    }
                });
            }
        }
        private ScrollDirection _scrollDirection = SCROLL_DIRECTIONS.Find(sd => sd.Value == (Orientation)(ApplicationData.Current.LocalSettings.Values[nameof(ScrollDirection)] ??= Orientation.Vertical));
        public ScrollDirection ScrollDirection {
            get => _scrollDirection;
            set {
                MainWindow.MainDispatcherQueue.TryEnqueue(() => {
                    if (SetProperty(ref _scrollDirection, value)) {
                        ApplicationData.Current.LocalSettings.Values[nameof(ScrollDirection)] = value.Value;
                    }
                });
            }
        }
        private int _imagesPerPage = (int)(ApplicationData.Current.LocalSettings.Values[nameof(ImagesPerPage)] ??= 0);
        public int ImagesPerPage {
            get => _imagesPerPage;
            set {
                MainWindow.MainDispatcherQueue.TryEnqueue(() => {
                    if (SetProperty(ref _imagesPerPage, value)) {
                        ApplicationData.Current.LocalSettings.Values[nameof(ImagesPerPage)] = value;
                    }
                });
            }
        }

        private static CommonSettings _main;
        public static CommonSettings Main => _main ??= new();
        private CommonSettings() {}
    }
}
