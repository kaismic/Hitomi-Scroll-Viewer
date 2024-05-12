using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Windows.System;
using static Hitomi_Scroll_Viewer.SearchPage;
using static Hitomi_Scroll_Viewer.Utils;

namespace Hitomi_Scroll_Viewer {
    public sealed partial class ImageWatchingPage : Page {
        private static MainWindow _mw;

        private static readonly string SCROLL_SPEED_TEXT = "Auto Scroll Speed";
        private static readonly string PAGE_TURN_DELAY_TEXT = "Auto Page Turn Delay";
        private static readonly (double min, double max) SCROLL_SPEED_RANGE = (0.001, 1);
        private static readonly (double min, double max) PAGE_TURN_DELAY_RANGE = (1, 10);
        private static readonly double SCROLL_SPEED_FREQ = 0.001;
        private static readonly double PAGE_TURN_DELAY_FREQ = 0.5;
        private static double _scrollSpeed = 0.05;
        private static double _pageTurnDelay = 5; // in seconds
        public bool IsAutoScrolling { private set; get; } = false;
        private bool _isLooping = true;

        private static int _currPage = 0;
        private Image[] _images;

        private bool _isLoaded = false;
        public enum ViewMode {
            Default,
            Scroll
        }
        private static ViewMode _viewMode = ViewMode.Default;
        public enum ViewDirection {
            TopToBottom,
            LeftToRight,
            RightToLeft
        }
        private static ViewDirection _viewDirection;
        private static int _numOfPages;

        private readonly object _actionLock = new();
        private bool _isInAction = false;

        private static readonly object _pageLock = new();

        public ImageWatchingPage(MainWindow mw) {
            _mw = mw;

            InitializeComponent();

            if (File.Exists(SETTINGS_PATH)) {
                Settings settings = (Settings)JsonSerializer.Deserialize(
                    File.ReadAllText(SETTINGS_PATH),
                    typeof(Settings),
                    serializerOptions
                );
                _viewMode = settings.viewMode;
                _viewDirection = settings.viewDirection;
                _scrollSpeed = settings.scrollSpeed;
                _numOfPages = settings.numOfPages;
                _pageTurnDelay = settings.pageTurnDelay;
                _isLooping = settings.isLooping;
            } else {
                _viewMode = ViewMode.Default;
                _viewDirection = ViewDirection.RightToLeft;
                _scrollSpeed = 0.05;
                _numOfPages = 1;
                _pageTurnDelay = 5;
                _isLooping = true;
            }
            switch (_viewMode) {
                case ViewMode.Default:
                    ViewModeBtn.Label = "Change to Scroll mode";
                    NumOfPagesSelector.IsEnabled = true;
                    MainScrollViewer.HorizontalScrollMode = ScrollMode.Disabled;
                    MainScrollViewer.VerticalScrollMode = ScrollMode.Disabled;
                    break;
                case ViewMode.Scroll:
                    ViewModeBtn.Label = "Change to Default mode";
                    NumOfPagesSelector.IsEnabled = false;
                    switch (_viewDirection) {
                        case ViewDirection.TopToBottom:
                            MainScrollViewer.HorizontalScrollMode = ScrollMode.Disabled;
                            MainScrollViewer.VerticalScrollMode = ScrollMode.Auto;
                            break;
                        case ViewDirection.LeftToRight or ViewDirection.RightToLeft:
                            MainScrollViewer.HorizontalScrollMode = ScrollMode.Auto;
                            MainScrollViewer.VerticalScrollMode = ScrollMode.Disabled;
                            break;
                    }
                    break;
            }
            SetScrollSpeedSlider();
            ViewDirectionSelector.SelectedIndex = (int)_viewDirection;
            NumOfPagesSelector.SelectedIndex = (_numOfPages - 1);
            LoopBtn.IsChecked = _isLooping;

            // handle mouse movement on commandbar
            TopCommandBar.PointerEntered += (_, _) => {
                TopCommandBar.IsOpen = true;
                TopCommandBar.Background.Opacity = 1;
                PageNumDisplay.Visibility = Visibility.Visible;
            };
            TopCommandBar.Closing += (_, _) => {
                TopCommandBar.Background.Opacity = 0;
                PageNumDisplay.Visibility = Visibility.Collapsed;
            };

            GoBackBtn.Click += (_, _) => _mw.SwitchPage();
            AutoScrollBtn.Click += (_, _) => StartStopAutoScroll((bool)AutoScrollBtn.IsChecked);
        }

        private void SetScrollSpeedSlider() {
            switch (_viewMode) {
                case ViewMode.Default:
                    // save pageTurnDelay value because ScrollSpeedSlider.Value resets when min max is set
                    double pageTurnDelay = _pageTurnDelay;
                    ScrollSpeedSlider.StepFrequency = PAGE_TURN_DELAY_FREQ;
                    ScrollSpeedSlider.TickFrequency = PAGE_TURN_DELAY_FREQ;
                    ScrollSpeedSlider.Minimum = PAGE_TURN_DELAY_RANGE.min;
                    ScrollSpeedSlider.Maximum = PAGE_TURN_DELAY_RANGE.max;
                    ScrollSpeedSlider.Header = PAGE_TURN_DELAY_TEXT;
                    ScrollSpeedSlider.Value = pageTurnDelay;
                    _pageTurnDelay = pageTurnDelay;
                    break;
                case ViewMode.Scroll:
                    // save scrollSpeed value because ScrollSpeedSlider.Value resets when min max is set
                    double scrollSpeed = _scrollSpeed;
                    ScrollSpeedSlider.StepFrequency = SCROLL_SPEED_FREQ;
                    ScrollSpeedSlider.TickFrequency = SCROLL_SPEED_FREQ;
                    ScrollSpeedSlider.Minimum = SCROLL_SPEED_RANGE.min;
                    ScrollSpeedSlider.Maximum = SCROLL_SPEED_RANGE.max;
                    ScrollSpeedSlider.Header = SCROLL_SPEED_TEXT;
                    ScrollSpeedSlider.Value = scrollSpeed;
                    _scrollSpeed = scrollSpeed;
                    break;
            }
        }

        private static readonly string GLYPH_CANCEL = "\xE711";

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

        private void HandleScrollViewChange(object _0, ScrollViewerViewChangingEventArgs _1) {
            if (_viewMode == ViewMode.Scroll) {
                _currPage = GetPageFromScrollOffset();
                SetPageText();
            }
        }

        private void HandleLoopBtnClick(object _0, RoutedEventArgs _1) {
            if ((bool)LoopBtn.IsChecked) {
                ShowActionIndicator(Symbol.RepeatAll, null);
            } else {
                ShowActionIndicator(Symbol.RepeatAll, GLYPH_CANCEL);
            }
        }

        private void HandleViewDirectionChange(object _0, SelectionChangedEventArgs _1) {
            _viewDirection = (ViewDirection)ViewDirectionSelector.SelectedIndex;
            UpdateImages();
            switch (_viewDirection) {
                case ViewDirection.TopToBottom:
                    ImageContainer.Orientation = Orientation.Vertical;
                    MainScrollViewer.HorizontalScrollMode = ScrollMode.Disabled;
                    MainScrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
                    MainScrollViewer.VerticalScrollMode = ScrollMode.Auto;
                    MainScrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;
                    break;
                case ViewDirection.LeftToRight or ViewDirection.RightToLeft:
                    ImageContainer.Orientation = Orientation.Horizontal;
                    MainScrollViewer.HorizontalScrollMode = ScrollMode.Auto;
                    MainScrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Visible;
                    MainScrollViewer.VerticalScrollMode = ScrollMode.Disabled;
                    MainScrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
                    break;
            }
        }

        private void HandleNumOfPagesChange(object _0, SelectionChangedEventArgs _1) {
            StartStopAction(true);
            _numOfPages = NumOfPagesSelector.SelectedIndex + 1;
            UpdateImages();
            StartStopAction(false);
        }

        private CancellationTokenSource _autoScrollCts = new();
        public void StartStopAutoScroll(bool starting) {
            IsAutoScrolling = starting;
            AutoScrollBtn.IsChecked = starting;
            stopwatch.Reset();
            if (starting) {
                ShowActionIndicator(Symbol.Play, null);
                AutoScrollBtn.Icon = new SymbolIcon(Symbol.Pause);
                AutoScrollBtn.Label = "Stop Auto Page Turning / Scrolling";
                _autoScrollCts = new();
                Task.Run(() => ScrollAutomatically(_autoScrollCts.Token), _autoScrollCts.Token);
            }
            else {
                ShowActionIndicator(Symbol.Pause, null);
                _autoScrollCts.Cancel();
                AutoScrollBtn.Icon = new SymbolIcon(Symbol.Play);
                AutoScrollBtn.Label = "Start Auto Page Turning / Scrolling";
            }
        }

        private void SetPageText() {
            PageNumText.Text = $"Page {_currPage} of {_mw.CurrLoadedGallery.files.Length - 1}";
        }

        private void InsertImages() {
            _currPage = (_currPage / _numOfPages) * _numOfPages;
            ImageContainer.Children.Clear();
            switch (_viewDirection) {
                case ViewDirection.TopToBottom or ViewDirection.LeftToRight:
                    for (int i = _currPage; i < Math.Min(_images.Length, _currPage + _numOfPages); i++) {
                        ImageContainer.Children.Add(_images[i]);
                    }
                    break;
                case ViewDirection.RightToLeft:
                    for (int i = Math.Min(_images.Length - 1, _currPage + _numOfPages - 1); i >= _currPage; i--) {
                        ImageContainer.Children.Add(_images[i]);
                    }
                    break;
            }
        }

        private void InsertAllImages() {
            ImageContainer.Children.Clear();
            switch (_viewDirection) {
                case ViewDirection.TopToBottom or ViewDirection.LeftToRight:
                    for (int i = 0; i < _images.Length; i++) {
                        ImageContainer.Children.Add(_images[i]);
                    }
                    break;
                case ViewDirection.RightToLeft:
                    for (int i = _images.Length - 1; i >= 0; i--) {
                        ImageContainer.Children.Add(_images[i]);
                    }
                    break;
            }
        }

        /**
         * <summary>Inserts images based on current page number</summary>
         */
        private async void UpdateImages() {
            if (_images == null) return;
            switch (_viewMode) {
                case ViewMode.Default:
                    InsertImages();
                    break;
                case ViewMode.Scroll:
                    int prevPage = _currPage;
                    InsertAllImages();
                    await WaitImageLoad();
                    _currPage = prevPage;
                    switch (_viewDirection) {
                        case ViewDirection.TopToBottom:
                            MainScrollViewer.DispatcherQueue.TryEnqueue(() => MainScrollViewer.ScrollToVerticalOffset(GetScrollOffsetFromPage()));
                            break;
                        case ViewDirection.LeftToRight or ViewDirection.RightToLeft:
                            MainScrollViewer.DispatcherQueue.TryEnqueue(() => MainScrollViewer.ScrollToHorizontalOffset(GetScrollOffsetFromPage()));
                            break;
                    }
                    break;
            }
            SetPageText();
        }

        private void HandleViewModeBtnClick(object _0, RoutedEventArgs _1) {
            StartStopAction(true);
            switch (_viewMode) {
                case ViewMode.Default:
                    _viewMode = ViewMode.Scroll;
                    ViewModeBtn.Label = "Change to Default mode";
                    NumOfPagesSelector.IsEnabled = false;
                    PageNavigator.IsEnabled = false;
                    switch (_viewDirection) {
                        case ViewDirection.TopToBottom:
                            MainScrollViewer.HorizontalScrollMode = ScrollMode.Disabled;
                            MainScrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
                            MainScrollViewer.VerticalScrollMode = ScrollMode.Auto;
                            MainScrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;

                            break;
                        case ViewDirection.LeftToRight or ViewDirection.RightToLeft:
                            MainScrollViewer.HorizontalScrollMode = ScrollMode.Auto;
                            MainScrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Visible;
                            MainScrollViewer.VerticalScrollMode = ScrollMode.Disabled;
                            MainScrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
                            break;
                    }
                    break;
                case ViewMode.Scroll:
                    _viewMode = ViewMode.Default;
                    ViewModeBtn.Label = "Change to Scroll mode";
                    NumOfPagesSelector.IsEnabled = true;
                    PageNavigator.IsEnabled  = true;
                    MainScrollViewer.HorizontalScrollMode = ScrollMode.Disabled;
                    MainScrollViewer.VerticalScrollMode = ScrollMode.Disabled;
                    break;
            }
            UpdateImages();
            SetScrollSpeedSlider();
            StartStopAction(false);
        }

        private void HandlePageSelectionChange(object _0, SelectionChangedEventArgs _1) {
            if (!pageChangedByUser) return;
            StartStopAction(true);
            _currPage = PageNavigator.SelectedIndex;
            UpdateImages();
            StartStopAction(false);
        }

        private async Task WaitImageLoad() {
            bool allLoaded = false;
            // wait for the images to be actually loaded into scrollview
            while (!allLoaded) {
                await Task.Delay(500);
                allLoaded = true;
                for (int i = 0; i < _images.Length; i++) {
                    if (_images[i].ActualWidth == 0) {
                        allLoaded = false;
                        break;
                    }
                }
            }
        }

        private int GetPageFromScrollOffset() {
            // half of the window height is the reference height for page calculation
            double pageHalfOffset = _viewDirection switch {
                ViewDirection.TopToBottom => MainScrollViewer.VerticalOffset + _mw.Bounds.Height / 2,
                ViewDirection.LeftToRight => MainScrollViewer.HorizontalOffset + _mw.Bounds.Width / 2,
                ViewDirection.RightToLeft => MainScrollViewer.ExtentWidth - (MainScrollViewer.HorizontalOffset + _mw.Bounds.Width / 2),
                _ => throw new NotImplementedException("Unhandled ViewDirection Mode")
            };
            double imageSizeSum = 0;
            switch (_viewDirection) {
                case ViewDirection.TopToBottom:
                    for (int i = 0; i < _images.Length; i++) {
                        imageSizeSum += _images[i].ActualHeight * MainScrollViewer.ZoomFactor;
                        if (imageSizeSum >= pageHalfOffset) {
                            return i;
                        }
                    }
                    break;
                case ViewDirection.LeftToRight or ViewDirection.RightToLeft:
                    for (int i = 0; i < _images.Length; i++) {
                        imageSizeSum += _images[i].ActualWidth * MainScrollViewer.ZoomFactor;
                        if (imageSizeSum >= pageHalfOffset) {
                            return i;
                        }
                    }
                    break;
            }
            return _images.Length - 1;
        }

        private double GetScrollOffsetFromPage() {
            if (_currPage == 0) {
                return _viewDirection switch {
                    ViewDirection.TopToBottom or ViewDirection.LeftToRight => 0,
                    ViewDirection.RightToLeft => MainScrollViewer.ScrollableWidth,
                    _ => throw new NotImplementedException("Unhandled ViewDirection Mode")
                };
            }
            double offset;
            switch (_viewDirection) {
                case ViewDirection.TopToBottom:
                    offset = 0;
                    for (int i = 0; i < _currPage; i++) {
                        offset += _images[i].ActualHeight * MainScrollViewer.ZoomFactor;
                    }
                    break;
                case ViewDirection.LeftToRight:
                    offset = 0;
                    for (int i = 0; i < _currPage; i++) {
                        offset += _images[i].ActualWidth * MainScrollViewer.ZoomFactor;
                    }
                    break;
                case ViewDirection.RightToLeft:
                    offset = MainScrollViewer.ScrollableWidth;
                    for (int i = _currPage - 1; i >= 0; i--) {
                        offset -= _images[i].ActualWidth * MainScrollViewer.ZoomFactor;
                    }
                    break;
                default:
                    throw new NotImplementedException("Unhandled ViewDirection Mode");
            }
            return offset;
        }

        private void SetScrollSpeed(object slider, RangeBaseValueChangedEventArgs _0) {
            switch (_viewMode) {
                case ViewMode.Default:
                    _pageTurnDelay = ((Slider)slider).Value;
                    break;
                case ViewMode.Scroll:
                    _scrollSpeed = ((Slider)slider).Value;
                    break;
            }
        }

        private bool pageChangedByUser = true;

        private void UpdatePageNum(int change) {
            lock (_pageLock) {
                pageChangedByUser = false;
                _currPage = (_currPage + change + _mw.CurrLoadedGallery.files.Length) % _mw.CurrLoadedGallery.files.Length;
                _currPage = (_currPage / _numOfPages) * _numOfPages;
                PageNavigator.SelectedIndex = _currPage;
                pageChangedByUser = true;
            }
        }

        public void HandleKeyDown(object _, KeyRoutedEventArgs e) {
            switch (e.Key) {
                case VirtualKey.L:
                    if ((bool)LoopBtn.IsChecked) {
                        LoopBtn.IsChecked = false;
                        ShowActionIndicator(Symbol.RepeatAll, GLYPH_CANCEL);
                    } else {
                        LoopBtn.IsChecked = true;
                        ShowActionIndicator(Symbol.RepeatAll, null);
                    }
                    break;
                case VirtualKey.Space:
                    if (!_isInAction && _isLoaded) {
                        StartStopAutoScroll(!IsAutoScrolling);
                    }
                    break;
                case VirtualKey.V:
                    if (!_isInAction && _isLoaded) HandleViewModeBtnClick(null, null);
                    break;
                case VirtualKey.Right or VirtualKey.Left:
                    if (!_isInAction && _isLoaded && _viewMode == ViewMode.Default) {
                        lock (_pageLock) {
                            switch (e.Key) {
                                case VirtualKey.Right:
                                    UpdatePageNum(_numOfPages);
                                    break;
                                case VirtualKey.Left:
                                    UpdatePageNum(-_numOfPages);
                                    break;
                            }
                            UpdateImages();
                        }
                    }
                    break;
                default:
                    break;
            }
        }

        // for updating auto scrolling in sync with real time
        private static readonly Stopwatch stopwatch = new();

        private async void ScrollAutomatically(CancellationToken ct) {
            while (IsAutoScrolling) {
                switch (_viewMode) {
                    case ViewMode.Default:
                        if (_currPage + 1 == _mw.CurrLoadedGallery.files.Length && !_isLooping) {
                            DispatcherQueue.TryEnqueue(() => StartStopAutoScroll(false));
                            return;
                        }
                        try {
                            await Task.Delay((int)(_pageTurnDelay * 1000), ct);
                        } catch (TaskCanceledException) {
                            return;
                        }
                        if (IsAutoScrolling) {
                            if (_currPage + 1 == _mw.CurrLoadedGallery.files.Length && !_isLooping) {
                                DispatcherQueue.TryEnqueue(() => StartStopAutoScroll(false));
                                return;
                            }
                            DispatcherQueue.TryEnqueue(() => { UpdatePageNum(_numOfPages); });
                            DispatcherQueue.TryEnqueue(UpdateImages);
                        }
                        break;
                    case ViewMode.Scroll:
                        try {
                            await Task.Delay(10, ct);
                        } catch (TaskCanceledException) {
                            return;
                        }
                        MainScrollViewer.DispatcherQueue.TryEnqueue(() => {
                            bool isEndOfPage = _viewDirection switch {
                                ViewDirection.TopToBottom => MainScrollViewer.VerticalOffset == MainScrollViewer.ScrollableHeight,
                                ViewDirection.LeftToRight => MainScrollViewer.HorizontalOffset == MainScrollViewer.ScrollableWidth,
                                ViewDirection.RightToLeft => MainScrollViewer.HorizontalOffset == 0,
                                _ => throw new NotImplementedException("Unhandled ViewDirection Mode")
                            };
                            if (isEndOfPage) {
                                if (_isLooping) {
                                    switch (_viewDirection) {
                                        case ViewDirection.TopToBottom:
                                            MainScrollViewer.ScrollToVerticalOffset(0);
                                            break;
                                        case ViewDirection.LeftToRight:
                                            MainScrollViewer.ScrollToHorizontalOffset(0);
                                            break;
                                        case ViewDirection.RightToLeft:
                                            MainScrollViewer.ScrollToHorizontalOffset(MainScrollViewer.ScrollableWidth);
                                            break;
                                    }
                                } else {
                                    StartStopAutoScroll(false);
                                    return;
                                }
                            }
                            else {
                                stopwatch.Stop();
                                switch (_viewDirection) {
                                    case ViewDirection.TopToBottom:
                                        MainScrollViewer.ScrollToVerticalOffset(MainScrollViewer.VerticalOffset + _scrollSpeed * stopwatch.ElapsedMilliseconds);
                                        break;
                                    case ViewDirection.LeftToRight:
                                        MainScrollViewer.ScrollToHorizontalOffset(MainScrollViewer.HorizontalOffset + _scrollSpeed * stopwatch.ElapsedMilliseconds);
                                        break;
                                    case ViewDirection.RightToLeft:
                                        MainScrollViewer.ScrollToHorizontalOffset(MainScrollViewer.HorizontalOffset - _scrollSpeed * stopwatch.ElapsedMilliseconds);
                                        break;
                                }
                                stopwatch.Restart();
                            }
                        });
                        break;
                }
            }
        }

        public void StartStopAction(bool start) {
            lock (_actionLock) {
                if (start) {
                    _isInAction = true;
                    if (IsAutoScrolling) StartStopAutoScroll(false);
                }
                EnableBookmarkLoading(!start);
                if (!start) {
                    _isInAction = false;
                }
            }
        }

        public void LoadGalleryFromLocalDir(Gallery gallery) {
            string imageDir = Path.Combine(IMAGE_DIR, gallery.id);
            if (!Directory.Exists(imageDir)) {
                return;
            }
            StartStopAction(true);
            LoadingProgressBar.Value = 0;
            LoadingProgressBar.Visibility = Visibility.Visible;
            ImageContainer.Children.Clear();
            _mw.CurrLoadedGallery = gallery;
            _images = new Image[gallery.files.Length];
            LoadingProgressBar.Maximum = gallery.files.Length;
            for (int i = 0; i < _images.Length; i++) {
                _images[i] = new();
                try {
                    string[] files = Directory.GetFiles(imageDir, i.ToString() + ".*");
                    if (files.Length > 0) {
                        _images[i].Source = new BitmapImage(new(files[0]));
                    }
                } catch (DirectoryNotFoundException) { }
                LoadingProgressBar.Value++;
            }
            LoadingProgressBar.Visibility = Visibility.Collapsed;
            _currPage = 0;
            PageNavigator.ItemsSource = Enumerable.Range(0, gallery.files.Length).ToList();
            UpdateImages();
            EnableControlButtonsOnLoad();
            _isLoaded = true;
            StartStopAction(false);
        }

        private void EnableControlButtonsOnLoad() {
            PageNavigator.IsEnabled = _viewMode == ViewMode.Default;
            ViewModeBtn.IsEnabled = true;
            ScrollSpeedSlider.IsEnabled = true;
            AutoScrollBtn.IsEnabled = true;
        }

        public Settings GetSettings() {
            return new(_viewMode, _viewDirection, _scrollSpeed, _numOfPages, _pageTurnDelay, _isLooping);
        }
    }
}
