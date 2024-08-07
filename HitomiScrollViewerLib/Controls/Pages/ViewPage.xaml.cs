﻿using HitomiScrollViewerLib.Controls.ViewPageComponents;
using HitomiScrollViewerLib.Entities;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.Windows.ApplicationModel.Resources;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.System;
using static HitomiScrollViewerLib.SharedResources;
using static HitomiScrollViewerLib.Utils;

namespace HitomiScrollViewerLib.Controls.Pages {
    public sealed partial class ViewPage : Page {
        private static readonly ResourceMap _resourceMap = MainResourceMap.GetSubtree("ViewPage");
        private readonly string[] ORIENTATION_NAMES = _resourceMap.GetValue("Text_StringArray_Orientation").ValueAsString.Split(',', StringSplitOptions.TrimEntries);
        private readonly string[] VIEW_DIRECTION_NAMES = _resourceMap.GetValue("Text_StringArray_ViewDirection").ValueAsString.Split(',', StringSplitOptions.TrimEntries);

        private static readonly string SCROLL_DIRECTION_SETTING_KEY = "ScrollDirection";
        private static readonly string VIEW_DIRECTION_SETTING_KEY = "ViewDirection";
        private static readonly string AUTO_SCROLL_INTERVAL_SETTING_KEY = "AutoScrollInterval";
        private static readonly string IS_LOOPING_SETTING_KEY = "IsLooping";
        private static readonly string USE_PAGE_FLIP_EFFECT_SETTING_KEY = "UsePageFlipEffect";
        private readonly ApplicationDataContainer _settings;

        private static readonly string GLYPH_CANCEL = "\xE711";

        private double _autoScrollInterval; // in seconds
        public bool IsAutoScrolling { get; private set; } = false;
        public Gallery CurrLoadedGallery { get; private set; }
        private readonly List<GroupedImagePanel> _groupedImagePanels = [];
        private readonly List<Range> _imgIndexRangesPerPage = [];

        private Orientation _scrollDirection;
        public enum ViewDirection {
            LeftToRight,
            RightToLeft
        }
        private ViewDirection _viewDirection;

        private static readonly object _actionLock = new();
        internal MainWindow MainWindow { get; set; }

        public ViewPage() {
            InitializeComponent();

            EnableControls(false);

            _settings = ApplicationData.Current.LocalSettings;
            _scrollDirection = (Orientation)(_settings.Values[SCROLL_DIRECTION_SETTING_KEY] ?? Orientation.Vertical);
            _viewDirection = (ViewDirection)(_settings.Values[VIEW_DIRECTION_SETTING_KEY] ?? ViewDirection.LeftToRight);
            _autoScrollInterval = (double)(_settings.Values[AUTO_SCROLL_INTERVAL_SETTING_KEY] ?? AutoScrollIntervalSlider.Minimum);
            LoopBtn.IsChecked = (bool)(_settings.Values[IS_LOOPING_SETTING_KEY] ?? true);
            UsePageFlipEffectCheckBox.IsChecked = ImageFlipView.UseTouchAnimationsForAllNavigation = ScrollDirectionSelector.IsEnabled = (bool)(_settings.Values[USE_PAGE_FLIP_EFFECT_SETTING_KEY] ?? true);

            AutoScrollIntervalSlider.Value = _autoScrollInterval;
            ViewDirectionSelector.SelectedIndex = (int)_viewDirection;
            ScrollDirectionSelector.SelectedIndex = (int)_scrollDirection;

            AutoScrollBtn.Label = TEXT_AUTO_SCROLL_BTN_OFF;
            LoopBtn.Label = (bool)LoopBtn.IsChecked ? TEXT_LOOP_BTN_ON : TEXT_LOOP_BTN_OFF;
            MoreSettingsContentDialog.CloseButtonText = TEXT_CLOSE;

            foreach (var control in TopCommandBar.PrimaryCommands.Cast<Control>()) {
                control.VerticalAlignment = VerticalAlignment.Stretch;
            }

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
            GoBackBtn.Click += (_, _) => MainWindow.SwitchPage();
            AutoScrollIntervalSlider.ValueChanged += (object sender, RangeBaseValueChangedEventArgs e) => { _autoScrollInterval = e.NewValue; };
            AutoScrollBtn.Click += (_, _) => ToggleAutoScroll((bool)AutoScrollBtn.IsChecked);
            LoopBtn.Click += (_, _) => SetLoopBtnStatus((bool)LoopBtn.IsChecked);
            UsePageFlipEffectCheckBox.Checked += (_, _) => ImageFlipView.UseTouchAnimationsForAllNavigation = ScrollDirectionSelector.IsEnabled = true;
            UsePageFlipEffectCheckBox.Unchecked += (_, _) => ImageFlipView.UseTouchAnimationsForAllNavigation = ScrollDirectionSelector.IsEnabled = false;
            MoreSettingsBtn.Click += async (_, _) => await MoreSettingsContentDialog.ShowAsync();

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
            _settings.Values[IS_LOOPING_SETTING_KEY] = LoopBtn.IsChecked;
            _settings.Values[USE_PAGE_FLIP_EFFECT_SETTING_KEY] = ImageFlipView.UseTouchAnimationsForAllNavigation;
        }

        private static readonly string Text_Notification_NoImagesToLoad_Title = _resourceMap.GetValue("Text_Notification_NoImagesToLoad_Title").ValueAsString;

        public async void LoadGallery(Gallery gallery) {
            if (!Directory.Exists(Path.Combine(IMAGE_DIR_V2, gallery.Id.ToString()))) {
                MainWindow.NotifyUser(Text_Notification_NoImagesToLoad_Title, "");
                return;
            }
            MainWindow.SwitchPage();
            if (ToggleAction(true)) {
                try {
                    if (CurrLoadedGallery != null && gallery.Id == CurrLoadedGallery.Id) {
                        await SetImageOrientationAndSize();
                        return;
                    }
                    CurrLoadedGallery = gallery;
                    await AddGroupedImagePanels(0);
                } finally {
                    ToggleAction(false);
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
            double currRemainingAspectRatio = viewportAspectRatio - (double)CurrLoadedGallery.Files[0].Width / CurrLoadedGallery.Files[0].Height;
            (int start, int end) currRange = (0, 1);
            for (int i = 1; i < CurrLoadedGallery.Files.Length; i++) {
                double imgAspectRatio = (double)CurrLoadedGallery.Files[i].Width / CurrLoadedGallery.Files[i].Height;
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
            await AttachPageEventHandlers(false);
            ImageFlipView.SelectedIndex = (int)rangeIndexIncludingPageIndex;
            PageNavigator.SelectedIndex = (int)rangeIndexIncludingPageIndex;
            await AttachPageEventHandlers(true);

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
            await AttachPageEventHandlers(false);
            int temp = ImageFlipView.SelectedIndex;
            (ImageFlipView.ItemsPanelRoot as VirtualizingStackPanel).Orientation = _scrollDirection;
            ImageFlipView.SelectedIndex = temp;
            await AttachPageEventHandlers(true);

            foreach (var panel in _groupedImagePanels) {
                panel.UpdateViewDirection(_viewDirection);
                panel.SetImageSizes(new(ImageFlipView.ActualWidth, ImageFlipView.ActualHeight));
            }
        }

        /**
         * <returns><c>true</c> if action is permitted, otherwise, <c>false</c></returns>
         */
        public bool ToggleAction(bool doOrFinishAction) {
            if (doOrFinishAction) {
                if (Monitor.TryEnter(_actionLock, 0)) {
                    EnableControls(false);
                    SearchPage.BookmarkItems.ForEach(bmItem => bmItem.EnableBookmarkClick(false));
                    return true;
                }
                return false;
            }
            EnableControls(true);
            SearchPage.BookmarkItems.ForEach(bmItem => bmItem.EnableBookmarkClick(true));
            Monitor.Exit(_actionLock);
            return true;
        }

        private void EnableControls(bool enable) {
            RefreshBtn.IsEnabled = enable;
            AutoScrollIntervalSlider.IsEnabled = enable;
            AutoScrollBtn.IsEnabled = enable;
            LoopBtn.IsEnabled = enable;
            PageNavigator.IsEnabled = enable;
            MoreSettingsBtn.IsEnabled = enable;
            if (IsAutoScrolling) ToggleAutoScroll(false);
        }

        private static string GetPageIndexText(Range range) {
            if (range.End.Value - range.Start.Value - 1 == 0) {
                return $"{range.Start.Value + 1}";
            } else {
                return $"{range.Start.Value + 1} - {range.End.Value}";
            }
        }

        private void SetCurrPageText(Range range) {
            PageNumTextBlock.Text = $"{GetPageIndexText(range)} / {CurrLoadedGallery.Files.Length}";
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

        private static readonly string TEXT_AUTO_SCROLL_BTN_ON = _resourceMap.GetValue("Text_AutoScrollBtn_On").ValueAsString;
        private static readonly string TEXT_AUTO_SCROLL_BTN_OFF = _resourceMap.GetValue("Text_AutoScrollBtn_Off").ValueAsString;

        private CancellationTokenSource _autoScrollCts = new();
        public void ToggleAutoScroll(bool starting) {
            IsAutoScrolling = starting;
            AutoScrollBtn.IsChecked = starting;
            stopwatch.Reset();
            if (starting) {
                ShowActionIndicator(Symbol.Play, null);
                AutoScrollBtn.Icon = new SymbolIcon(Symbol.Pause);
                AutoScrollBtn.Label = TEXT_AUTO_SCROLL_BTN_ON;
                _autoScrollCts = new();
                Task.Run(() => ScrollAutomatically(_autoScrollCts.Token), _autoScrollCts.Token);
            } else {
                ShowActionIndicator(Symbol.Pause, null);
                _autoScrollCts.Cancel();
                AutoScrollBtn.Icon = new SymbolIcon(Symbol.Play);
                AutoScrollBtn.Label = TEXT_AUTO_SCROLL_BTN_OFF;
            }
        }

        // for updating auto scrolling in sync with real time
        private static readonly Stopwatch stopwatch = new();

        private async void ScrollAutomatically(CancellationToken ct) {
            while (IsAutoScrolling) {
                DispatcherQueue.TryEnqueue(() => {
                    if (!(bool)LoopBtn.IsChecked && ImageFlipView.SelectedIndex == ImageFlipView.Items.Count - 1) {
                        ToggleAutoScroll(false);
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
                        if (!(bool)LoopBtn.IsChecked && ImageFlipView.SelectedIndex == ImageFlipView.Items.Count - 1) {
                            ToggleAutoScroll(false);
                            return;
                        }
                        ImageFlipView.SelectedIndex = (ImageFlipView.SelectedIndex + 1) % ImageFlipView.Items.Count;
                    }
                });
            }
        }

        private static readonly string TEXT_LOOP_BTN_ON = _resourceMap.GetValue("Text_LoopBtn_On").ValueAsString;
        private static readonly string TEXT_LOOP_BTN_OFF = _resourceMap.GetValue("Text_LoopBtn_Off").ValueAsString;

        private void SetLoopBtnStatus(bool on) {
            LoopBtn.IsChecked = on;
            if (on) {
                ShowActionIndicator(Symbol.RepeatAll, null);
                LoopBtn.Label = TEXT_LOOP_BTN_ON;
            } else {
                ShowActionIndicator(Symbol.RepeatAll, GLYPH_CANCEL);
                LoopBtn.Label = TEXT_LOOP_BTN_OFF;
            }
        }

        public void ViewPage_PreviewKeyDown(object _, KeyRoutedEventArgs e) {
            if (CurrLoadedGallery == null) return;
            switch (e.Key) {
                case VirtualKey.L:
                    e.Handled = true;
                    bool isChecked = (bool)LoopBtn.IsChecked;
                    LoopBtn.IsChecked = !isChecked;
                    SetLoopBtnStatus(!isChecked);
                    break;
                case VirtualKey.Space:
                    e.Handled = true;
                    if (!Monitor.IsEntered(_actionLock)) {
                        ToggleAutoScroll(!IsAutoScrolling);
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

        private async Task AttachPageEventHandlers(bool attach) {
            if (attach) {
                await Task.Delay(200);
                ImageFlipView.SelectionChanged += ImageFlipView_SelectionChanged;
                PageNavigator.SelectionChanged += PageNavigator_SelectionChanged;
            } else {
                ImageFlipView.SelectionChanged -= ImageFlipView_SelectionChanged;
                PageNavigator.SelectionChanged -= PageNavigator_SelectionChanged;
                await Task.Delay(200);
            }
        }

        private async void ImageFlipView_SelectionChanged(object _0, SelectionChangedEventArgs e) {
            if (e.RemovedItems.Count == 0 || ImageFlipView.SelectedItem == null || Monitor.IsEntered(_actionLock)) {
                return;
            }
            await AttachPageEventHandlers(false);
            PageNavigator.SelectedIndex = ImageFlipView.SelectedIndex;
            SetCurrPageText(_imgIndexRangesPerPage[ImageFlipView.SelectedIndex]);
            await AttachPageEventHandlers(true);
        }

        private async void RefreshBtn_Clicked(object _0, RoutedEventArgs _1) {
            ShowActionIndicator(Symbol.Refresh, null);
            if (ToggleAction(true)) {
                try {
                    string imageDir = Path.Combine(IMAGE_DIR_V2, CurrLoadedGallery.Id.ToString());
                    if (!Directory.Exists(imageDir)) {
                        return;
                    }
                    foreach (var panel in _groupedImagePanels) {
                        panel.RefreshImages();
                    }
                    await SetImageOrientationAndSize();
                } finally {
                    ToggleAction(false);
                }
            }
        }

        private async void ScrollDirectionSelector_SelectionChanged(object _0, SelectionChangedEventArgs e) {
            _scrollDirection = (Orientation)ScrollDirectionSelector.SelectedIndex;
            if (ImageFlipView.ItemsPanelRoot != null) {
                int temp = ImageFlipView.SelectedIndex;
                await AttachPageEventHandlers(false);
                (ImageFlipView.ItemsPanelRoot as VirtualizingStackPanel).Orientation = _scrollDirection;
                await Task.Delay(100);
                ImageFlipView.SelectedIndex = temp;
                await AttachPageEventHandlers(true);
            }
        }

        private async void ViewDirectionSelector_SelectionChanged(object _0, SelectionChangedEventArgs e) {
            if (e.RemovedItems.Count == 0) {
                return;
            }
            _viewDirection = (ViewDirection)ViewDirectionSelector.SelectedIndex;
            int temp = ImageFlipView.SelectedIndex;
            await AttachPageEventHandlers(false);
            foreach (var panel in _groupedImagePanels) {
                panel.UpdateViewDirection(_viewDirection);
            }
            ImageFlipView.SelectedIndex = temp;
            await AttachPageEventHandlers(true);
        }

        private async void PageNavigator_SelectionChanged(object _0, SelectionChangedEventArgs e) {
            if (e.RemovedItems.Count == 0 || PageNavigator.SelectedItem == null || Monitor.IsEntered(_actionLock)) {
                return;
            }
            await AttachPageEventHandlers(false);
            ImageFlipView.SelectedIndex = PageNavigator.SelectedIndex;
            SetCurrPageText(_imgIndexRangesPerPage[PageNavigator.SelectedIndex]);
            await AttachPageEventHandlers(true);
        }

        private DateTime _lastWindowSizeChangeTime;
        public async void Window_SizeChanged() {
            DateTime thisDateTime = _lastWindowSizeChangeTime = DateTime.UtcNow;
            // wait for a short time to check if there is a later SizeChanged event to prevent unnecessary rapid method calls
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
