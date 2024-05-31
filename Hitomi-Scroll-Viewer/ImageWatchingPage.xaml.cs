using Hitomi_Scroll_Viewer.ImageWatchingPageComponent;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.System;
using static Hitomi_Scroll_Viewer.Utils;

namespace Hitomi_Scroll_Viewer {
    public sealed partial class ImageWatchingPage : Page {
        private static readonly string SCROLL_DIRECTION_SETTING_KEY = "ScrollDirection";
        private static readonly string VIEW_DIRECTION_SETTING_KEY = "ViewDirection";
        private static readonly string NUM_OF_PAGES_SETTING_KEY = "NumOfPages";
        private static readonly string AUTO_SCROLL_INTERVAL_SETTING_KEY = "AutoScrollInterval";
        private static readonly string IS_LOOPING_SETTING_KEY = "IsLooping";
        private readonly ApplicationDataContainer _settings;

        private static readonly string GLYPH_CANCEL = "\xE711";

        private static readonly (double min, double max) PAGE_TURN_DELAY_RANGE = (1, 10);
        private static readonly double PAGE_TURN_DELAY_FREQ = 0.5;
        private static double _autoScrollInterval; // in seconds
        public bool IsAutoScrolling { private set; get; } = false;
        private bool _isLooping = true;
        public Gallery CurrLoadedGallery { get; set; }
        private readonly ObservableCollection<Image> _images = [];
        private readonly ObservableCollection<GroupedImagePanel> _groupedImagePanels = [];

        private DateTime _lastWindowSizeChangeTime;

        private static Orientation _scrollDirection;
        public enum ViewDirection {
            TopToBottom,
            LeftToRight,
            RightToLeft
        }
        private static ViewDirection _viewDirection;

        private static int _currPageIndex;
        private static int _numOfPages;

        private bool _isInAction = false;
        private bool _pageChangedBySystem = false;

        public ImageWatchingPage() {
            InitializeComponent();

            EnableControls(false);

            _settings = ApplicationData.Current.LocalSettings;
            _scrollDirection = (Orientation)(_settings.Values[SCROLL_DIRECTION_SETTING_KEY] ?? Orientation.Horizontal);
            _viewDirection = (ViewDirection)(_settings.Values[VIEW_DIRECTION_SETTING_KEY] ?? ViewDirection.TopToBottom);
            _numOfPages = (int)(_settings.Values[NUM_OF_PAGES_SETTING_KEY] ?? 1);
            _autoScrollInterval = (double)(_settings.Values[AUTO_SCROLL_INTERVAL_SETTING_KEY] ?? (PAGE_TURN_DELAY_RANGE.min + PAGE_TURN_DELAY_RANGE.max) / 2);
            _isLooping = (bool)(_settings.Values[IS_LOOPING_SETTING_KEY] ?? true);

            AutoScrollIntervalSlider.StepFrequency = PAGE_TURN_DELAY_FREQ;
            AutoScrollIntervalSlider.TickFrequency = PAGE_TURN_DELAY_FREQ;
            AutoScrollIntervalSlider.Minimum = PAGE_TURN_DELAY_RANGE.min;
            AutoScrollIntervalSlider.Maximum = PAGE_TURN_DELAY_RANGE.max;

            ViewDirectionSelector.SelectedIndex = (int)_viewDirection;
            NumOfPagesSelector.SelectedIndex = _numOfPages - 1;
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
            AutoScrollIntervalSlider.ValueChanged += (_, _) => { _autoScrollInterval = AutoScrollIntervalSlider.Value; };
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
            _settings.Values[NUM_OF_PAGES_SETTING_KEY] = _numOfPages;
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

                    _images.Clear();

                    PageNavigator.ItemsSource = Enumerable.Range(1, gallery.files.Length + 1).ToList();
                    PageNavigator.SelectedIndex = 0;
                    SetCurrPageIndex(0);
                    for (int i = 0; i < gallery.files.Length; i++) {
                        Image image = new();
                        string[] files = Directory.GetFiles(imageDir, i.ToString() + ".*");
                        if (files.Length > 0) {
                            image.Source = new BitmapImage(new(files[0]));
                        }
                        _images.Add(image);
                    }
                    await RefreshLayout();
                } finally {
                    StartStopAction(false);
                }
            }
        }

        private async Task RefreshLayout() {
            foreach (var panel in _groupedImagePanels) {
                panel.Children.Clear();
            }
            _groupedImagePanels.Clear();
            /*
                example:
                _images.Count = 22, _numOfPages = 4
                22 / 4 = 5 r 2
                -----------------
                _groupedImagePanels = [0 ~ 3, 4 ~ 7, 8 ~ 11, 12 ~ 15, 16 ~ 19, 20 ~ 21]
            */
            for (int i = 0; i < _images.Count / _numOfPages; i++) {
                Range range = (i * _numOfPages)..(Math.Min((i + 1) * _numOfPages, _images.Count));
                _groupedImagePanels.Add(new(_viewDirection, _images.Take(range), CurrLoadedGallery.files.Take(range)));
            }
            bool prevPageChangedBySystem = _pageChangedBySystem;
            _pageChangedBySystem = true;
            ImageFlipView.SelectedIndex = _currPageIndex / _numOfPages;
            SetCurrPageIndex(_currPageIndex);
            _pageChangedBySystem = prevPageChangedBySystem;
            await SetImageOrientationAndSize();
        }

        private async Task SetImageOrientationAndSize() {
            foreach (var image in _images) {
                image.Width = double.NaN;
                image.Height = double.NaN;
                if (image.Source != null) {
                    (image.Source as BitmapImage).DecodePixelWidth = 0;
                    (image.Source as BitmapImage).DecodePixelHeight = 0;
                }
            }
            while (ImageFlipView.ActualWidth == 0 || ImageFlipView.ActualWidth == 0) {
                await Task.Delay(100);
            }
            while (ImageFlipView.ItemsPanelRoot == null) {
                await Task.Delay(100);
            }
            (ImageFlipView.ItemsPanelRoot as VirtualizingStackPanel).Orientation = _scrollDirection;

            foreach (var panel in _groupedImagePanels) {
                panel.UpdateViewDirection(_viewDirection);
                panel.SetImageSizes(_viewDirection, new(ImageFlipView.ActualWidth, ImageFlipView.ActualHeight));
            }
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
            NumOfPagesSelector.IsEnabled = enable;
            PageNavigator.IsEnabled = enable;
            if (IsAutoScrolling) StartStopAutoScroll(false);
        }

        private void SetCurrPageIndex(int idx) {
            bool prevPageChangedBySystem = _pageChangedBySystem;
            _pageChangedBySystem = true;
            _currPageIndex = idx;
            PageNumText.Text = $"{idx + 1} of {CurrLoadedGallery.files.Length}";
            PageNavigator.SelectedIndex = idx;
            _pageChangedBySystem = prevPageChangedBySystem;
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

        private void ImageFlipView_SelectionChanged(object _0, SelectionChangedEventArgs e) {
            if (e.RemovedItems.Count == 0 || _pageChangedBySystem || ImageFlipView.SelectedItem == null || _isInAction) {
                return;
            }
            bool prevPageChangedBySystem = _pageChangedBySystem;
            _pageChangedBySystem = true;
            SetCurrPageIndex(ImageFlipView.SelectedIndex * _numOfPages);
            _pageChangedBySystem = prevPageChangedBySystem;
        }

        private async void RefreshBtn_Clicked(object _0, RoutedEventArgs _1) {
            ShowActionIndicator(Symbol.Refresh, null);
            if (StartStopAction(true)) {
                try {
                    string imageDir = Path.Combine(IMAGE_DIR, CurrLoadedGallery.id);
                    if (!Directory.Exists(imageDir)) {
                        return;
                    }
                    for (int i = 0; i < _images.Count; i++) {
                        Image image = _images[i];
                        if (image.Source == null) {
                            string[] files = Directory.GetFiles(imageDir, i.ToString() + ".*");
                            if (files.Length > 0) {
                                image.Source = new BitmapImage(new(files[0]));
                            }
                        }
                    }
                    await SetImageOrientationAndSize();
                } finally {
                    StartStopAction(false);
                }
            }
        }

        private void ScrollDirectionSelector_SelectionChanged(object _0, SelectionChangedEventArgs e) {
            // TODO
        }

        private void LoopBtn_Clicked(object _0, RoutedEventArgs _1) {
            if ((bool)LoopBtn.IsChecked) {
                ShowActionIndicator(Symbol.RepeatAll, null);
            } else {
                ShowActionIndicator(Symbol.RepeatAll, GLYPH_CANCEL);
            }
        }

        private async void ViewDirectionSelector_SelectionChanged(object _0, SelectionChangedEventArgs e) {
            if (e.RemovedItems.Count == 0) {
                return;
            }
            _viewDirection = (ViewDirection)ViewDirectionSelector.SelectedIndex;
            await SetImageOrientationAndSize();
        }

        private async void NumOfPagesSelector_SelectionChanged(object _0, SelectionChangedEventArgs e) {
            if (NumOfPagesSelector.SelectedItem == null) {
                return;
            }
            _numOfPages = (int)NumOfPagesSelector.SelectedItem;
            if (e.RemovedItems.Count == 0) {
                return;
            }
            if (StartStopAction(true)) {
                try {
                    await RefreshLayout();
                } finally {
                    StartStopAction(false);
                }
            }
        }

        private void PageNavigator_SelectionChanged(object _0, SelectionChangedEventArgs e) {
            if (e.RemovedItems.Count == 0 || _pageChangedBySystem || PageNavigator.SelectedItem == null || _isInAction) {
                return;
            }
            bool prevPageChangedBySystem = _pageChangedBySystem;
            _pageChangedBySystem = true;
            ImageFlipView.SelectedIndex = PageNavigator.SelectedIndex / _numOfPages;
            SetCurrPageIndex(ImageFlipView.SelectedIndex * _numOfPages);
            _pageChangedBySystem = prevPageChangedBySystem;
        }

        public async void Window_SizeChanged() {
            DateTime thisDateTime = _lastWindowSizeChangeTime = DateTime.Now;
            // wait for a short time to check if there is a later SizeChanged event to prevent rapid CheckAndUpdateImageSources() calls
            await Task.Delay(200);
            if (_lastWindowSizeChangeTime != thisDateTime) {
                return;
            }
            if (CurrLoadedGallery != null) {
                await SetImageOrientationAndSize();
            }
        }
    }
}
