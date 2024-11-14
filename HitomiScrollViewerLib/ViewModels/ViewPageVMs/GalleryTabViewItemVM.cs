using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HitomiScrollViewerLib.Entities;
using HitomiScrollViewerLib.Models;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using static HitomiScrollViewerLib.Constants;

namespace HitomiScrollViewerLib.ViewModels.ViewPageVMs {
    public partial class GalleryTabViewItemVM : DQObservableObject {
        private readonly ImageInfo[] _imageInfos;
        public Gallery Gallery { get; }
        public string TabHeader => Gallery.Id + " - " + Gallery.Title;
        public string NonVirtualImageDirPath { get; private init; }
        public GalleryViewSettings GalleryViewSettings { get; } = new();
        public CommonSettings CommonSettings { get; } = CommonSettings.Main;

        private Size _currentTabViewSize;
        public Size CurrentTabViewSize {
            get => _currentTabViewSize;
            set {
                if (value.Width == _currentTabViewSize.Width && value.Height == _currentTabViewSize.Height) {
                    return;
                }
                _currentTabViewSize = value;
                UpdateImageCollectionPanelVMs();
            }
        }
        private const int SIZE_CHANGE_WAIT_TIME = 200;
        private DateTime _lastSizeChangedTime;

        [ObservableProperty]
        private List<ImageCollectionPanelVM> _imageCollectionPanelVMs;
        partial void OnImageCollectionPanelVMsChanged(List<ImageCollectionPanelVM> value) {
            _ = Task.Run(async () => {
                await Task.Delay(100);
                FlipViewSelectedIndex = -1;
                FlipViewSelectedIndex = 0;
            });
        }

        [ObservableProperty]
        private int _flipViewSelectedIndex;

        private CancellationTokenSource _autoScrollCts;

        [ObservableProperty]
        private SymbolIcon _autoScrollButtonIcon = new(Symbol.Play);
        [ObservableProperty]
        private string _autoScrollButtonLabel = "Start Auto Scrolling";

        [ObservableProperty]
        private bool _isAutoScrolling = false;
        partial void OnIsAutoScrollingChanged(bool value) {
            if (value) {
                AutoScrollButtonIcon = new(Symbol.Pause);
                AutoScrollButtonLabel = "Stop Auto Scrolling";
                ShowActionIconRequested?.Invoke(GLYPH_PLAY, null);
                _autoScrollCts = new();
                Task.Run(() => StartAutoScrolling(_autoScrollCts.Token), _autoScrollCts.Token);
            } else {
                AutoScrollButtonIcon = new(Symbol.Play);
                AutoScrollButtonLabel = "Start Auto Scrolling";
                ShowActionIconRequested?.Invoke(GLYPH_PAUSE, null);
                _autoScrollCts.Cancel();
            }
        }

        private async void StartAutoScrolling(CancellationToken ct) {
            while (IsAutoScrolling) {
                try {
                    await Task.Delay((int)(GalleryViewSettings.AutoScrollInterval * 1000), ct);
                } catch (TaskCanceledException) {
                    return;
                }
                if (!GalleryViewSettings.IsLoopEnabled && FlipViewSelectedIndex == ImageCollectionPanelVMs.Count - 1) {
                    IsAutoScrolling = false;
                    return;
                } else {
                    FlipViewSelectedIndex = (FlipViewSelectedIndex + 1) % ImageCollectionPanelVMs.Count;
                }
            }
        }

        public event Action<string, string> ShowActionIconRequested;

        public GalleryTabViewItemVM(Gallery gallery) {
            _imageInfos = [.. gallery.Files.OrderBy(f => f.Index)];
            Gallery = gallery;
            NonVirtualImageDirPath = Path.Combine(NON_VIRTUAL_IMAGE_DIR_V3, gallery.Id.ToString());
            GalleryViewSettings.PropertyChanged += GalleryViewSettings_PropertyChanged;
            CommonSettings.PropertyChanged += CommonSettings_PropertyChanged;
            RefreshCommand = new(UpdateImageCollectionPanelVMs);
            UpdateImageCollectionPanelVMs();
        }

        private void CommonSettings_PropertyChanged(object _0, PropertyChangedEventArgs e) {
            if (e.PropertyName == nameof(CommonSettings.ImagesPerPage)) {
                UpdateImageCollectionPanelVMs();
            }
        }

        private void GalleryViewSettings_PropertyChanged(object _0, PropertyChangedEventArgs e) {
            if (e.PropertyName == nameof(GalleryViewSettings.IsLoopEnabled)) {
                if (GalleryViewSettings.IsLoopEnabled) {
                    ShowActionIconRequested?.Invoke(GLYPH_REPEAT_ALL, null);
                } else {
                    ShowActionIconRequested?.Invoke(GLYPH_REPEAT_ALL, GLYPH_CANCEL);
                }
            }
        }

        public RelayCommand RefreshCommand { get; }

        public async void UpdateImageCollectionPanelVMs() {
            IsAutoScrolling = false;
            DateTime localRecordedTime = _lastSizeChangedTime = DateTime.Now;
            await Task.Delay(SIZE_CHANGE_WAIT_TIME);
            if (_lastSizeChangedTime != localRecordedTime) {
                return;
            }
            int imagesPerPage = CommonSettings.Main.ImagesPerPage;
            if (imagesPerPage == 0) {
                // auto allocate images per page by aspect ratio
                List<ImageCollectionPanelVM> imageCollectionPanelVMs = [];
                double viewportAspectRatio = CurrentTabViewSize.Width / CurrentTabViewSize.Height;
                double remainingAspectRatio = viewportAspectRatio - ((double)_imageInfos[0].Width / _imageInfos[0].Height);
                Range currentRange = 0..1;
                int pageIndex = 0;
                for (int i = 1; i < _imageInfos.Length; i++) {
                    double imgAspectRatio = (double)_imageInfos[i].Width / _imageInfos[i].Height;
                    if (imgAspectRatio >= remainingAspectRatio) {
                        imageCollectionPanelVMs.Add(GetImageCollectionPanelVM(pageIndex++, _imageInfos[currentRange]));
                        remainingAspectRatio = viewportAspectRatio;
                        currentRange = i..(i + 1);
                    } else {
                        remainingAspectRatio -= imgAspectRatio;
                        currentRange = currentRange.Start..(i + 1);
                    }
                }
                // add last range
                imageCollectionPanelVMs.Add(GetImageCollectionPanelVM(pageIndex++, _imageInfos[currentRange]));
                ImageCollectionPanelVMs = imageCollectionPanelVMs;
            } else {
                // otherwise add according to ImagesPerPage
                int vmsCount = (int)Math.Ceiling((double)_imageInfos.Length / imagesPerPage);
                ImageCollectionPanelVM[] imageCollectionPanelVMs = new ImageCollectionPanelVM[vmsCount];
                for (int i = 0; i < vmsCount; i++) {
                    int start = i * imagesPerPage;
                    int end = Math.Min((i + 1) * imagesPerPage, _imageInfos.Length);
                    imageCollectionPanelVMs[i] = GetImageCollectionPanelVM(i, _imageInfos[start..end]);
                }
                ImageCollectionPanelVMs = [.. imageCollectionPanelVMs];
            }
        }

        public ImageCollectionPanelVM GetImageCollectionPanelVM(int pageIndex, ImageInfo[] imageInfos) {
            return new() {
                PageIndex = pageIndex,
                GalleryId = Gallery.Id,
                SizeAdjustedImageInfos =
                    imageInfos
                    .Select(
                        info => new SizeAdjustedImageInfo() {
                            FullFileName = info.FullFileName,
                            ImageFilePath = info.ImageFilePath,
                            Height = CurrentTabViewSize.Height,
                            Width = CurrentTabViewSize.Height * info.Width / info.Height,
                            IsPlayable = info.IsPlayable
                        }
                    )
                    .ToArray(),
                CommonSettings = CommonSettings,
                GalleryViewSettings = GalleryViewSettings
            };
        }
    }
}