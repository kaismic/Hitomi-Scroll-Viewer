using Hitomi_Scroll_Viewer.SearchPageComponent;
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
using System.Net.Http;
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
        private static readonly (double, double) SCROLL_SPEED_RANGE = (0.001, 1);
        private static readonly (double, double) PAGE_TURN_DELAY_RANGE = (1, 10);
        private static readonly double SCROLL_SPEED_FREQ = 0.001;
        private static readonly double PAGE_TURN_DELAY_FREQ = 0.5;
        private static double _scrollSpeed = 0.05;
        private static double _pageTurnDelay = 5; // in seconds
        public bool IsAutoScrolling { private set; get; } = false;
        private bool _isLooping = true;

        private static int _currPage = 0;
        private Image[] _images;

        private readonly int[] _downloadThreadNums = [1, 2, 3, 4, 5, 6, 7, 8];
        private int _downloadThreadNum = 1;

        public enum GalleryState {
            Bookmarked,
            Bookmarking,
            Loaded,
            Empty
        }
        private static GalleryState _galleryState = GalleryState.Empty;
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

        private CancellationTokenSource _cts;
        private readonly object _actionLock = new();
        private bool _isInAction = false;

        private static readonly Mutex _pageMutex = new();

        // galleries for testing
        // https://hitomi.la/doujinshi/kameki-%E6%97%A5%E6%9C%AC%E8%AA%9E-2561144.html#1
        // https://hitomi.la/doujinshi/radiata-%E6%97%A5%E6%9C%AC%E8%AA%9E-2472850.html#1

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

            BookmarkBtn.Click += (_, _) => {
                ChangeBookmarkBtnState(GalleryState.Bookmarking);
                _mw.sp.AddBookmark(_mw.gallery);
                ChangeBookmarkBtnState(GalleryState.Bookmarked);
            };
        }

        private void SetScrollSpeedSlider() {
            switch (_viewMode) {
                case ViewMode.Default:
                    // save pageTurnDelay value because ScrollSpeedSlider.Value resets when min max is set
                    double pageTurnDelay = _pageTurnDelay;
                    ScrollSpeedSlider.StepFrequency = PAGE_TURN_DELAY_FREQ;
                    ScrollSpeedSlider.TickFrequency = PAGE_TURN_DELAY_FREQ;
                    ScrollSpeedSlider.Minimum = PAGE_TURN_DELAY_RANGE.Item1;
                    ScrollSpeedSlider.Maximum = PAGE_TURN_DELAY_RANGE.Item2;
                    ScrollSpeedSlider.Header = PAGE_TURN_DELAY_TEXT;
                    ScrollSpeedSlider.Value = pageTurnDelay;
                    _pageTurnDelay = pageTurnDelay;
                    break;
                case ViewMode.Scroll:
                    // save scrollSpeed value because ScrollSpeedSlider.Value resets when min max is set
                    double scrollSpeed = _scrollSpeed;
                    ScrollSpeedSlider.StepFrequency = SCROLL_SPEED_FREQ;
                    ScrollSpeedSlider.TickFrequency = SCROLL_SPEED_FREQ;
                    ScrollSpeedSlider.Minimum = SCROLL_SPEED_RANGE.Item1;
                    ScrollSpeedSlider.Maximum = SCROLL_SPEED_RANGE.Item2;
                    ScrollSpeedSlider.Header = SCROLL_SPEED_TEXT;
                    ScrollSpeedSlider.Value = scrollSpeed;
                    _scrollSpeed = scrollSpeed;
                    break;
            }
        }

        private void ShowActionIndicator(Symbol symbol) {
            ActionIndicatorSymbol.Symbol = symbol;
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
                ShowActionIndicator(Symbol.RepeatAll);
            } else {
                ShowActionIndicator(Symbol.DisableUpdates);
            }
        }

        private void HandleGoBackBtnClick(object _0, RoutedEventArgs _1) {
            _mw.SwitchPage();
        }

        private void HandleAutoScrollBtnClick(object _0, RoutedEventArgs _1) {
            StartStopAutoScroll((bool)AutoScrollBtn.IsChecked);
        }

        private void HandleLoadingControlBtnClick(object _0, RoutedEventArgs _1) {
            if (_isInAction) {
                LoadingControlBtn.IsEnabled = false;
                _cts.Cancel();
            } else {
                if (_mw.gallery != null) {
                    ReloadGallery();
                }
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
            _numOfPages = NumOfPagesSelector.SelectedIndex + 1;
            UpdateImages();
        }

        private async void ReloadGallery() {
            ContentDialog dialog = new() {
                Title = "Reload only the missing images?",
                PrimaryButtonText = "Yes",
                SecondaryButtonText = "No, Reload all images",
                CloseButtonText = "Cancel",
                XamlRoot = XamlRoot
            };
            ContentDialogResult cdr = await dialog.ShowAsync();

            bool reloadAll = false;
            switch (cdr) {
                // reload only the missing images
                case ContentDialogResult.Primary:
                    reloadAll = false;
                    break;
                // reload all images
                case ContentDialogResult.Secondary:
                    reloadAll = true;
                    break;
                case ContentDialogResult.None:
                    return;
            }

            StartLoading();

            // create new cts
            _cts = new();
            CancellationToken ct = _cts.Token;

            int[] missingIndexes;

            if (reloadAll) {
                missingIndexes = Enumerable.Range(0, _mw.gallery.files.Length).ToArray();
            } else {
                try {
                    missingIndexes = GetMissingIndexes(_mw.gallery);
                    if (missingIndexes.Length == 0) {
                        _mw.AlertUser("There are no missing images", "");
                        FinishLoading(_galleryState);
                        return;
                    }
                } catch (DirectoryNotFoundException) {
                    missingIndexes = Enumerable.Range(0, _mw.gallery.files.Length).ToArray();
                }
            }

            if (ct.IsCancellationRequested) {
                FinishLoading(_galleryState);
                return;
            }

            string serverTime = null;
            try {
                serverTime = await GetServerTime(_mw.httpClient, ct);
            } catch (HttpRequestException e) {
                _mw.AlertUser("An error has occurred while getting server time. Please try again.", e.Message);
            } catch (TaskCanceledException) {
                FinishLoading(_galleryState);
                return;
            }

            ImageInfo[] imageInfos = new ImageInfo[missingIndexes.Length];
            for (int i = 0; i < missingIndexes.Length; i++) {
                imageInfos[i] = _mw.gallery.files[missingIndexes[i]];
            }
            string[] imgFormats = GetImageFormats(imageInfos);
            string[] imgAddresses = GetImageAddresses(imageInfos, imgFormats, serverTime);

            if (ct.IsCancellationRequested) {
                FinishLoading(_galleryState);
                return;
            }

            LoadingProgressBar.Maximum = missingIndexes.Length;

            Task[] tasks = DownloadImages(
                _mw.httpClient,
                _mw.gallery.id,
                imgAddresses,
                imgFormats,
                missingIndexes,
                _downloadThreadNum,
                LoadingProgressBar,
                ct
            );

            try {
                await Task.WhenAll(tasks);
            } catch (TaskCanceledException) {
                FinishLoading(_galleryState);
                return;
            }

            int[] stillMissingIndexes = GetMissingIndexes(_mw.gallery);
            int[] loadedIndexes = missingIndexes.Except(stillMissingIndexes).ToArray();
            string imageDir = Path.Combine(IMAGE_DIR, _mw.gallery.id);
            for (int i = 0; i < loadedIndexes.Length; i++) {
                if (ct.IsCancellationRequested) {
                    FinishLoading(_galleryState);
                    return;
                }
                int idx = loadedIndexes[i];
                string[] file = Directory.GetFiles(imageDir, idx.ToString() + ".*");
                _images[idx].Source = new BitmapImage(new(file[0]));
            }

            if (stillMissingIndexes.Length > 0) {
                _mw.AlertUser($"Failed to download {stillMissingIndexes.Length} images.", "Try reducing thread number if the problem persists.");
            } else {
                _mw.AlertUser($"Reloading finished successfully", _mw.gallery.title);
            }

            BookmarkItem bmItem = GetBookmarkItem(_mw.gallery.id);
            if (bmItem != null) {
                bmItem.ReloadImages();
                FinishLoading(GalleryState.Bookmarked);
                return;
            }
            FinishLoading(GalleryState.Loaded);
        }

        private CancellationTokenSource _autoScrollCts = new();
        public void StartStopAutoScroll(bool starting) {
            IsAutoScrolling = starting;
            AutoScrollBtn.IsChecked = starting;
            stopwatch.Reset();
            if (starting) {
                ShowActionIndicator(Symbol.Play);
                AutoScrollBtn.Icon = new SymbolIcon(Symbol.Pause);
                AutoScrollBtn.Label = "Stop Auto Page Turning / Scrolling";
                _autoScrollCts = new();
                Task.Run(() => ScrollAutomatically(_autoScrollCts.Token), _autoScrollCts.Token);
            }
            else {
                ShowActionIndicator(Symbol.Pause);
                _autoScrollCts.Cancel();
                AutoScrollBtn.Icon = new SymbolIcon(Symbol.Play);
                AutoScrollBtn.Label = "Start Auto Page Turning / Scrolling";
            }
        }

        private void SetPageText() {
            PageNumText.Text = $"Page {_currPage} of {_mw.gallery.files.Length - 1}";
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
                    MainScrollViewer.HorizontalScrollMode = ScrollMode.Disabled;
                    MainScrollViewer.VerticalScrollMode = ScrollMode.Disabled;
                    break;
            }
            UpdateImages();
            SetScrollSpeedSlider();
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

        private void SetScrollSpeed(object slider, RangeBaseValueChangedEventArgs e) {
            switch (_viewMode) {
                case ViewMode.Default:
                    _pageTurnDelay = ((Slider)slider).Value;
                    break;
                case ViewMode.Scroll:
                    _scrollSpeed = ((Slider)slider).Value;
                    break;
            }
        }

        private static void UpdatePageNum(int num) {
            _pageMutex.WaitOne();
            _currPage = (_currPage + num + _mw.gallery.files.Length) % _mw.gallery.files.Length;
            _pageMutex.ReleaseMutex();
        }

        public void HandleKeyDown(object _, KeyRoutedEventArgs e) {
            switch (e.Key) {
                case VirtualKey.L:
                    if ((bool)LoopBtn.IsChecked) {
                        LoopBtn.IsChecked = false;
                        ShowActionIndicator(Symbol.DisableUpdates);
                    } else {
                        LoopBtn.IsChecked = true;
                        ShowActionIndicator(Symbol.RepeatAll);
                    }
                    break;
                case VirtualKey.Space:
                    if (!_isInAction && _galleryState != GalleryState.Empty) {
                        StartStopAutoScroll(!IsAutoScrolling);
                    }
                    break;
                case VirtualKey.V:
                    if (!_isInAction && _galleryState != GalleryState.Empty) HandleViewModeBtnClick(null, null);
                    break;
                case VirtualKey.Right or VirtualKey.Left:
                    if (!_isInAction && _galleryState != GalleryState.Empty && _viewMode == ViewMode.Default) {
                        _pageMutex.WaitOne();
                        switch (e.Key) {
                            case VirtualKey.Right:
                                UpdatePageNum(_numOfPages);
                                break;
                            case VirtualKey.Left:
                                UpdatePageNum(-_numOfPages);
                                break;
                        }
                        UpdateImages();
                        _pageMutex.ReleaseMutex();
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
                        if (_currPage + 1 == _mw.gallery.files.Length && !_isLooping) {
                            DispatcherQueue.TryEnqueue(() => StartStopAutoScroll(false));
                            return;
                        }
                        try {
                            await Task.Delay((int)(_pageTurnDelay * 1000), ct);
                        } catch (TaskCanceledException) {
                            return;
                        }
                        if (IsAutoScrolling) {
                            if (_currPage + 1 == _mw.gallery.files.Length && !_isLooping) {
                                DispatcherQueue.TryEnqueue(() => StartStopAutoScroll(false));
                                return;
                            }
                            UpdatePageNum(_numOfPages);
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
                    LoadingControlBtn.Label = "Cancel Loading";
                    LoadingControlBtn.Icon = new SymbolIcon(Symbol.Cancel);
                    LoadingControlBtn.IsEnabled = true;
                    if (IsAutoScrolling) StartStopAutoScroll(false);
                }
                _mw.sp.EnableLoading(!start);
                if (!start) {
                    if (_galleryState != GalleryState.Empty) {
                        ViewModeBtn.IsEnabled = true;
                        ScrollSpeedSlider.IsEnabled = true;
                        AutoScrollBtn.IsEnabled = true;
                    }
                    if (_galleryState != GalleryState.Empty) {
                        LoadingControlBtn.Label = "Reload Gallery " + _mw.gallery.id;
                        LoadingControlBtn.Icon = new SymbolIcon(Symbol.Sync);
                        LoadingControlBtn.IsEnabled = true;
                    }
                    _isInAction = false;
                }
            }
        }

        private void StartLoading() {
            StartStopAction(true);
            LoadingProgressBar.Visibility = Visibility.Visible;
            LoadingProgressBar.Value = 0;
        }

        private void FinishLoading(GalleryState state) {
            LoadingProgressBar.Visibility = Visibility.Collapsed;
            ChangeBookmarkBtnState(state);
            StartStopAction(false);
        }

        public void LoadGalleryFromLocalDir(Gallery gallery) {
            StartLoading();
            // delete previous gallery if not bookmarked
            if (_mw.gallery != null) {
                if (GetBookmarkItem(_mw.gallery.id) == null) {
                    DeleteGallery(_mw.gallery);
                }
            }
            _mw.gallery = gallery;
            _images = new Image[gallery.files.Length];
            LoadingProgressBar.Maximum = gallery.files.Length;
            string imageDir = Path.Combine(IMAGE_DIR, gallery.id);
            for (int i = 0; i < _images.Length; i++) {
                _images[i] = new();
                try {
                    string[] file = Directory.GetFiles(imageDir, i.ToString() + ".*");
                    if (file.Length > 0) {
                        _images[i].Source = new BitmapImage(new(file[0]));
                    }
                } catch (DirectoryNotFoundException) { }
                LoadingProgressBar.Value++;
            }
            _currPage = 0;
            UpdateImages();
            FinishLoading(GalleryState.Bookmarked);
        }

        public void ChangeBookmarkBtnState(GalleryState state) {
            _galleryState = state;
            if (state == GalleryState.Loaded) {
                BookmarkBtn.IsEnabled = true;
            }
            else {
                BookmarkBtn.IsEnabled = false;
            }
            switch (state) {
                case GalleryState.Bookmarked:
                    BookmarkBtn.Label = "Bookmarked";
                    break;
                case GalleryState.Bookmarking:
                    BookmarkBtn.Label = "Bookmarking...";
                    break;
                case GalleryState.Loaded:
                    BookmarkBtn.Label = "Bookmark this gallery";
                    break;
                case GalleryState.Empty:
                    BookmarkBtn.Label = "";
                    break;
            }
        }

        public bool IsBusy() {
            lock (_actionLock) if (_isInAction) {
                    return true;
            }
            return false;
        }

        public Settings GetSettings() {
            return new(_viewMode, _viewDirection, _scrollSpeed, _numOfPages, _pageTurnDelay, _isLooping);
        }
    }
}
