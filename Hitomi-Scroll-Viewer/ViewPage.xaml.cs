﻿using Hitomi_Scroll_Viewer.ImageWatchingPageComponent;
using Microsoft.OpenApi.Extensions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.System;
using static Hitomi_Scroll_Viewer.Utils;

namespace Hitomi_Scroll_Viewer {
    public sealed partial class ViewPage : Page {
        private static readonly string SCROLL_DIRECTION_SETTING_KEY = "ScrollDirection";
        private static readonly string VIEW_DIRECTION_SETTING_KEY = "ViewDirection";
        private static readonly string AUTO_SCROLL_INTERVAL_SETTING_KEY = "AutoScrollInterval";
        private static readonly string IS_LOOPING_SETTING_KEY = "IsLooping";
        private readonly ApplicationDataContainer _settings;

        private static readonly string GLYPH_CANCEL = "\xE711";

        private readonly Range AUTO_SCROLL_INTERVAL_RANGE = 1..10;
        private readonly double AUTO_SCROLL_INTERVAL_FREQ = 0.5;
        private double _autoScrollInterval; // in seconds
        public bool IsAutoScrolling { get; private set; } = false;
        private bool _isLooping = true;
        public Gallery CurrLoadedGallery { get; private set; }
        private readonly ItemsChangeObservableCollection<GroupedImagePanel> _groupedImagePanels = [];
        private readonly List<Range> _imgIndexRangesPerPage = [];

        private DateTime _lastWindowSizeChangeTime;

        private Orientation _scrollDirection;
        private readonly string[] ORIENTATION_NAMES = Enum.GetNames(typeof(Orientation));
        public enum ViewDirection {
            [Description("Left to Right")]
            LeftToRight,
            [Description("Right to Left")]
            RightToLeft
        }
        private ViewDirection _viewDirection;
        private readonly IEnumerable<string> VIEW_DIRECTION_NAMES = Enum.GetValues(typeof(ViewDirection))
            .Cast<ViewDirection>()
            .Select(viewDirection => EnumExtensions.GetAttributeOfType<DescriptionAttribute>(viewDirection).Description);

        private bool _isInAction = false;

        public ViewPage() {
            InitializeComponent();

            EnableControls(false);

            _settings = ApplicationData.Current.LocalSettings;
            _scrollDirection = (Orientation)(_settings.Values[SCROLL_DIRECTION_SETTING_KEY] ?? Orientation.Horizontal);
            _viewDirection = (ViewDirection)(_settings.Values[VIEW_DIRECTION_SETTING_KEY] ?? ViewDirection.LeftToRight);
            _autoScrollInterval = (double)(_settings.Values[AUTO_SCROLL_INTERVAL_SETTING_KEY] ?? (AUTO_SCROLL_INTERVAL_RANGE.Start.Value + AUTO_SCROLL_INTERVAL_RANGE.End.Value) / 2.0);
            _isLooping = (bool)(_settings.Values[IS_LOOPING_SETTING_KEY] ?? true);

            AutoScrollIntervalSlider.Value = _autoScrollInterval;
            ViewDirectionSelector.SelectedIndex = (int)_viewDirection;
            ScrollDirectionSelector.SelectedIndex = (int)_scrollDirection;
            LoopBtn.IsChecked = _isLooping;

            TopCommandBar.PointerEntered += (_, _) => {
                TopCommandBar.Opacity = 1;
                TopCommandBar.IsOpen = true;
                PageNumDisplay.Visibility = Visibility.Visible;
            };
            TopCommandBar.Opening += (_, _) => {
                TopCommandBar.Opacity = 1;
                PageNumDisplay.Visibility = Visibility.Visible;
            };
            TopCommandBar.Closing += (_, _) => {
                TopCommandBar.Opacity = 0;
                PageNumDisplay.Visibility = Visibility.Collapsed;
            };
            GoBackBtn.Click += (_, _) => App.MainWindow.SwitchPage();
            AutoScrollIntervalSlider.ValueChanged += (object sender, RangeBaseValueChangedEventArgs e) => { _autoScrollInterval = e.NewValue; };
            AutoScrollBtn.Click += (_, _) => StartStopAutoScroll((bool)AutoScrollBtn.IsChecked);

            // remove _flipview navigation buttons
            void FlipView_Loaded(object sender, RoutedEventArgs e) {
                ImageFlipView.Loaded -= FlipView_Loaded;
                Grid flipViewGrid = VisualTreeHelper.GetChild(ImageFlipView, 0) as Grid;
                var children = flipViewGrid.Children;
                for (int i = children.Count - 1; i >= 0; i--) {
                    if (children[i] is Button) {
                        children.RemoveAt(i);
                    }
                }
            }
            ImageFlipView.Loaded += FlipView_Loaded;
        }

        public void SaveSettings() {
            _settings.Values[SCROLL_DIRECTION_SETTING_KEY] = (int)_scrollDirection;
            _settings.Values[VIEW_DIRECTION_SETTING_KEY] = (int)_viewDirection;
            _settings.Values[AUTO_SCROLL_INTERVAL_SETTING_KEY] = _autoScrollInterval;
            _settings.Values[IS_LOOPING_SETTING_KEY] = _isLooping;
        }

        public async void LoadGallery(Gallery gallery) {
            App.MainWindow.SwitchPage();
            if (StartStopAction(true)) {
                try {
                    if (CurrLoadedGallery != null && gallery.id == CurrLoadedGallery.id) {
                        await SetImageOrientationAndSize();
                        return;
                    }
                    string imageDir = Path.Combine(IMAGE_DIR, gallery.id);
                    if (!Directory.Exists(imageDir)) {
                        return;
                    }
                    CurrLoadedGallery = gallery;

                    await AddGroupedImagePanels(0);
                } finally {
                    StartStopAction(false);
                }
            }
        }

        private async Task AddGroupedImagePanels(int pageIndex) {
            _imgIndexRangesPerPage.Clear();
            PageNavigator.Items.Clear();
            ImageFlipView.Items.Clear();
            foreach (var panel in _groupedImagePanels) {
                panel.Children.Clear();
            }
            _groupedImagePanels.Clear();

            while (ImageFlipView.ActualWidth == 0 || ImageFlipView.ActualHeight == 0) {
                await Task.Delay(100);
            }

            int? rangeIndexIncludingPageIndex = null;
            // create _groupedImagePanels with size-adaptative image number allocation per page
            double viewportAspectRatio = ImageFlipView.ActualWidth / ImageFlipView.ActualHeight;
            double currRemainingAspectRatio = viewportAspectRatio - (double)CurrLoadedGallery.files[0].width / CurrLoadedGallery.files[0].height;
            (int start, int end) currRange = (0, 1);
            for (int i = 1; i < CurrLoadedGallery.files.Length; i++) {
                double imgAspectRatio = (double)CurrLoadedGallery.files[i].width / CurrLoadedGallery.files[i].height;
                if (imgAspectRatio >= currRemainingAspectRatio) {
                    if (currRange.start <= pageIndex && pageIndex < currRange.end) {
                        rangeIndexIncludingPageIndex = _imgIndexRangesPerPage.Count;
                    }
                    AddRangeItems(currRange.start..currRange.end);
                    currRange = (i, i);
                    currRemainingAspectRatio = viewportAspectRatio;
                }
                currRemainingAspectRatio -= imgAspectRatio;
                currRange.end++;
            }
            // add last range
            AddRangeItems(currRange.start..currRange.end);

            SetCurrPageText(_imgIndexRangesPerPage[(int)rangeIndexIncludingPageIndex]);
            AttachPageEventHandlers(false);
            await Task.Delay(200);
            ImageFlipView.SelectedIndex = (int)rangeIndexIncludingPageIndex;
            PageNavigator.SelectedIndex = (int)rangeIndexIncludingPageIndex;
            await Task.Delay(200);
            AttachPageEventHandlers(true);

            await SetImageOrientationAndSize();
        }

        private void AddRangeItems(Range range) {
            _imgIndexRangesPerPage.Add(range);
            GroupedImagePanel panel = new(_viewDirection, range, CurrLoadedGallery);
            _groupedImagePanels.Add(panel);
            ImageFlipView.Items.Add(panel);
            PageNavigator.Items.Add(GetPageIndexText(range));
        }

        private async Task SetImageOrientationAndSize() {
            foreach (var panel in _groupedImagePanels) {
                panel.ResetImageSizes();
            }
            while (ImageFlipView.ActualWidth == 0 || ImageFlipView.ActualWidth == 0) {
                await Task.Delay(100);
            }
            while (ImageFlipView.ItemsPanelRoot == null) {
                await Task.Delay(100);
            }
            AttachPageEventHandlers(false);
            await Task.Delay(200);
            int temp = ImageFlipView.SelectedIndex;
            (ImageFlipView.ItemsPanelRoot as VirtualizingStackPanel).Orientation = _scrollDirection;
            ImageFlipView.SelectedIndex = temp;
            await Task.Delay(200);
            AttachPageEventHandlers(true);

            foreach (var panel in _groupedImagePanels) {
                panel.UpdateViewDirection(_viewDirection);
                panel.SetImageSizes(new(ImageFlipView.ActualWidth, ImageFlipView.ActualHeight));
            }
            _groupedImagePanels.NotifyItemChange();
        }

        /**
         * <returns><c>true</c> if action is permitted, otherwise, <c>false</c></returns>
         */
        public bool StartStopAction(bool doOrFinishAction) {
            if (doOrFinishAction) {
                if (!_isInAction) {
                    _isInAction = true;
                    EnableControls(false);
                    SearchPage.BookmarkItems.ForEach(bmItem => bmItem.EnableBookmarkClick(false));
                    return true;
                }
                return false;
            }
            EnableControls(true);
            SearchPage.BookmarkItems.ForEach(bmItem => bmItem.EnableBookmarkClick(true));
            _isInAction = false;
            return true;
        }

        private void EnableControls(bool enable) {
            RefreshBtn.IsEnabled = enable;
            AutoScrollIntervalSlider.IsEnabled = enable;
            AutoScrollBtn.IsEnabled = enable;
            LoopBtn.IsEnabled = enable;
            ScrollDirectionSelector.IsEnabled = enable;
            ViewDirectionSelector.IsEnabled = enable;
            PageNavigator.IsEnabled = enable;
            if (IsAutoScrolling) StartStopAutoScroll(false);
        }

        private static string GetPageIndexText(Range range) {
            if (range.End.Value - range.Start.Value - 1 == 0) {
                return $"{range.Start.Value + 1}";
            } else {
                return $"{range.Start.Value + 1} - {range.End.Value}";
            }
        }

        private void SetCurrPageText(Range range) {
            PageNumText.Text = $"{GetPageIndexText(range)} of {CurrLoadedGallery.files.Length}";
        }

        private void ShowActionIndicator(Symbol? symbol, string glyph) {
            if (symbol == null) {
                ActionIndicatorSymbolIcon.Opacity = 0;
            } else {
                ActionIndicatorSymbolIcon.Symbol = symbol.Value;
                ActionIndicatorSymbolIcon.Opacity = 1;
            }
            if (glyph == null) {
                ActionIndicatorFontIcon.Opacity = 0;
            } else {
                ActionIndicatorFontIcon.Glyph = glyph;
                ActionIndicatorFontIcon.Opacity = 1;
            }
            FadeOutStoryboard.Begin();
        }

        private CancellationTokenSource _autoScrollCts = new();
        public void StartStopAutoScroll(bool starting) {
            IsAutoScrolling = starting;
            AutoScrollBtn.IsChecked = starting;
            stopwatch.Reset();
            if (starting) {
                ShowActionIndicator(Symbol.Play, null);
                AutoScrollBtn.Icon = new SymbolIcon(Symbol.Pause);
                AutoScrollBtn.Label = "Stop Auto Scrolling";
                _autoScrollCts = new();
                Task.Run(() => ScrollAutomatically(_autoScrollCts.Token), _autoScrollCts.Token);
            } else {
                ShowActionIndicator(Symbol.Pause, null);
                _autoScrollCts.Cancel();
                AutoScrollBtn.Icon = new SymbolIcon(Symbol.Play);
                AutoScrollBtn.Label = "Start Auto Scrolling";
            }
        }

        // for updating auto scrolling in sync with real time
        private static readonly Stopwatch stopwatch = new();

        private async void ScrollAutomatically(CancellationToken ct) {
            while (IsAutoScrolling) {
                DispatcherQueue.TryEnqueue(() => {
                    if (!_isLooping && ImageFlipView.SelectedIndex == ImageFlipView.Items.Count - 1) {
                        StartStopAutoScroll(false);
                        return;
                    }
                });
                try {
                    await Task.Delay((int)(_autoScrollInterval * 1000), ct);
                } catch (TaskCanceledException) {
                    return;
                }
                DispatcherQueue.TryEnqueue(() => {
                    if (IsAutoScrolling) {
                        if (!_isLooping && ImageFlipView.SelectedIndex == ImageFlipView.Items.Count - 1) {
                            StartStopAutoScroll(false);
                            return;
                        }
                        ImageFlipView.SelectedIndex = (ImageFlipView.SelectedIndex + 1) % ImageFlipView.Items.Count;
                    }
                });
            }
        }

        public void ImageWatchingPage_PreviewKeyDown(object _, KeyRoutedEventArgs e) {
            if (CurrLoadedGallery == null) return;
            switch (e.Key) {
                case VirtualKey.L:
                    e.Handled = true;
                    if ((bool)LoopBtn.IsChecked) {
                        LoopBtn.IsChecked = false;
                        ShowActionIndicator(Symbol.RepeatAll, GLYPH_CANCEL);
                    } else {
                        LoopBtn.IsChecked = true;
                        ShowActionIndicator(Symbol.RepeatAll, null);
                    }
                    break;
                case VirtualKey.Space:
                    e.Handled = true;
                    if (!_isInAction) {
                        StartStopAutoScroll(!IsAutoScrolling);
                    }
                    break;
                case VirtualKey.Left or VirtualKey.Up:
                    e.Handled = true;
                    if (ImageFlipView.SelectedIndex == 0) {
                        ImageFlipView.SelectedIndex = ImageFlipView.Items.Count - 1;
                    } else {
                        ImageFlipView.SelectedIndex--;
                    }
                    break;
                case VirtualKey.Right or VirtualKey.Down:
                    e.Handled = true;
                    if (ImageFlipView.SelectedIndex == ImageFlipView.Items.Count - 1) {
                        ImageFlipView.SelectedIndex = 0;
                    } else {
                        ImageFlipView.SelectedIndex++;
                    }
                    break;
                default:
                    break;
            }
        }

        private void AttachPageEventHandlers(bool attach) {
            if (attach) {
                ImageFlipView.SelectionChanged += ImageFlipView_SelectionChanged;
                PageNavigator.SelectionChanged += PageNavigator_SelectionChanged;
            } else {
                ImageFlipView.SelectionChanged -= ImageFlipView_SelectionChanged;
                PageNavigator.SelectionChanged -= PageNavigator_SelectionChanged;
            }
        }

        private async void ImageFlipView_SelectionChanged(object _0, SelectionChangedEventArgs e) {
            if (e.RemovedItems.Count == 0 || ImageFlipView.SelectedItem == null || _isInAction) {
                return;
            }
            AttachPageEventHandlers(false);
            await Task.Delay(200);
            PageNavigator.SelectedIndex = ImageFlipView.SelectedIndex;
            SetCurrPageText(_imgIndexRangesPerPage[ImageFlipView.SelectedIndex]);
            await Task.Delay(200);
            AttachPageEventHandlers(true);
        }

        private async void RefreshBtn_Clicked(object _0, RoutedEventArgs _1) {
            ShowActionIndicator(Symbol.Refresh, null);
            if (StartStopAction(true)) {
                try {
                    string imageDir = Path.Combine(IMAGE_DIR, CurrLoadedGallery.id);
                    if (!Directory.Exists(imageDir)) {
                        return;
                    }
                    foreach (var panel in _groupedImagePanels) {
                        panel.RefreshImages();
                    }
                    await SetImageOrientationAndSize();
                } finally {
                    StartStopAction(false);
                }
            }
        }

        private void LoopBtn_Clicked(object _0, RoutedEventArgs _1) {
            if ((bool)LoopBtn.IsChecked) {
                ShowActionIndicator(Symbol.RepeatAll, null);
            } else {
                ShowActionIndicator(Symbol.RepeatAll, GLYPH_CANCEL);
            }
        }

        private async void ScrollDirectionSelector_SelectionChanged(object _0, SelectionChangedEventArgs e) {
            _scrollDirection = (Orientation)ScrollDirectionSelector.SelectedIndex;
            if (ImageFlipView.ItemsPanelRoot != null) {
                AttachPageEventHandlers(false);
                int temp = ImageFlipView.SelectedIndex;
                (ImageFlipView.ItemsPanelRoot as VirtualizingStackPanel).Orientation = _scrollDirection;
                await Task.Delay(200);
                ImageFlipView.SelectedIndex = temp;
                AttachPageEventHandlers(true);
            }
        }

        private async void ViewDirectionSelector_SelectionChanged(object _0, SelectionChangedEventArgs e) {
            if (e.RemovedItems.Count == 0) {
                return;
            }
            _viewDirection = (ViewDirection)ViewDirectionSelector.SelectedIndex;
            AttachPageEventHandlers(false);
            int temp = ImageFlipView.SelectedIndex;
            foreach (var panel in _groupedImagePanels) {
                panel.UpdateViewDirection(_viewDirection);
            }
            await Task.Delay(200);
            ImageFlipView.SelectedIndex = temp;
            AttachPageEventHandlers(true);
        }

        private async void PageNavigator_SelectionChanged(object _0, SelectionChangedEventArgs e) {
            if (e.RemovedItems.Count == 0 || PageNavigator.SelectedItem == null || _isInAction) {
                return;
            }
            AttachPageEventHandlers(false);
            await Task.Delay(200);
            ImageFlipView.SelectedIndex = PageNavigator.SelectedIndex;
            SetCurrPageText(_imgIndexRangesPerPage[PageNavigator.SelectedIndex]);
            await Task.Delay(200);
            AttachPageEventHandlers(true);
        }

        public async void Window_SizeChanged() {
            DateTime thisDateTime = _lastWindowSizeChangeTime = DateTime.Now;
            // wait for a short time to check if there is a later SizeChanged event to prevent rapid CheckAndUpdateImageSources() calls
            await Task.Delay(200);
            if (_lastWindowSizeChangeTime != thisDateTime) {
                return;
            }
            if (CurrLoadedGallery != null) {
                await AddGroupedImagePanels(_imgIndexRangesPerPage[ImageFlipView.SelectedIndex].Start.Value);
            }
        }
    }
}