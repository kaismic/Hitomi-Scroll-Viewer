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
using static Hitomi_Scroll_Viewer.MainWindow;
using static Hitomi_Scroll_Viewer.SearchPage;
using static Hitomi_Scroll_Viewer.Utils;

namespace Hitomi_Scroll_Viewer {
    public sealed partial class ImageWatchingPage : Page {
        private static MainWindow _mw;

        private static readonly string SCROLL_SPEED_TEXT = "Auto Scroll Speed";
        private static readonly string PAGE_TURN_DELAY_TEXT = "Auto Page Turn Delay";
        private static readonly (double, double) SCROLL_SPEED_RANGE = (0.001, 0.5);
        private static readonly (double, double) PAGE_TURN_DELAY_RANGE = (1, 10);
        private static readonly double SCROLL_SPEED_FREQ = 0.001;
        private static readonly double PAGE_TURN_DELAY_FREQ = 0.5;
        private static double _scrollSpeed = 0.05;
        private static double _pageTurnDelay = 5; // in seconds
        private bool _isAutoScrolling = false;
        private bool _isLooping = true;

        private static int _currPage = 0;
        private Image[] _images;

        private readonly int[] _downloadThreadNums = new int[] { 1, 2, 3, 4 };
        private int _downloadThreadNum = 1;

        public enum GalleryState {
            Bookmarked,
            Bookmarking,
            BookmarkFull,
            Loaded,
            Empty
        }
        private static GalleryState _galleryState = GalleryState.Empty;
        private enum ViewMode {
            Default,
            Scroll
        }
        private static ViewMode _viewMode = ViewMode.Default;
        private enum ScrollDirection {
            TopToBottom,
            LeftToRight,
            RightToLeft
        }
        private static ScrollDirection _scrollDirection;

        private CancellationTokenSource _cts;
        private readonly object _actionLock = new();
        private bool _isInAction = false;

        private static readonly Mutex _pageMutex = new();
        private readonly ManualResetEventSlim bookmarkSignal = new(true);

        // galleries for testing
        // https://hitomi.la/doujinshi/kameki-%E6%97%A5%E6%9C%AC%E8%AA%9E-2561144.html#1
        // https://hitomi.la/doujinshi/radiata-%E6%97%A5%E6%9C%AC%E8%AA%9E-2472850.html#1

        public ImageWatchingPage(MainWindow mw) {
            _mw = mw;

            InitializeComponent();

            SetScrollSpeedSlider();

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
            TopCommandBar.PointerCaptureLost += (_, _) => { TopCommandBar.IsOpen = false; };
            TopCommandBar.PointerCanceled += (_, _) => { TopCommandBar.IsOpen = false; };
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

        private void HandleScrollViewChange(object _0, ScrollViewerViewChangingEventArgs _1) {
            if (_viewMode == ViewMode.Scroll) {
                _currPage = GetPageFromScrollOffset();
                SetPageText();
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

        private async void ChangeScrollDirection(object _0, SelectionChangedEventArgs _1) {
            if (_images == null) return;
            _scrollDirection = (ScrollDirection)ScrollDirectionSelector.SelectedIndex;
            int prevPage = _currPage;
            InsertImages();
            await WaitImageLoad();
            _currPage = prevPage;
            SetPageText();
            switch (_scrollDirection) {
                case ScrollDirection.TopToBottom:
                    ImageContainer.Orientation = Orientation.Vertical;
                    MainScrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;
                    MainScrollViewer.VerticalScrollMode = ScrollMode.Enabled;
                    MainScrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
                    MainScrollViewer.HorizontalScrollMode = ScrollMode.Disabled;
                    MainScrollViewer.DispatcherQueue.TryEnqueue(() => MainScrollViewer.ScrollToVerticalOffset(GetScrollOffsetFromPage()));
                    break;
                case ScrollDirection.LeftToRight or ScrollDirection.RightToLeft:
                    ImageContainer.Orientation = Orientation.Horizontal;
                    MainScrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
                    MainScrollViewer.VerticalScrollMode = ScrollMode.Disabled;
                    MainScrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Visible;
                    MainScrollViewer.HorizontalScrollMode = ScrollMode.Enabled;
                    MainScrollViewer.DispatcherQueue.TryEnqueue(() => MainScrollViewer.ScrollToHorizontalOffset(GetScrollOffsetFromPage()));
                    break;
            }
        }

        // TODO animation when space or loop btn pressed show the corresponding icon animation which fades out

        private async void ReloadGallery() {
            ContentDialog dialog = new() {
                IsPrimaryButtonEnabled = true,
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
            CancellationTokenSource localCts = new();
            CancellationToken ct = localCts.Token;
            _cts = localCts;

            string imageDir = Path.Combine(IMAGE_DIR, _mw.gallery.id);

            Directory.CreateDirectory(imageDir);

            int[] missingIndexes;

            if (reloadAll) {
                missingIndexes = Enumerable.Range(0, _mw.gallery.files.Length).ToArray();
            } else {
                missingIndexes = GetMissingIndexes(_mw.gallery);
                if (missingIndexes.Length == 0) {
                    _mw.AlertUser("There are no missing images", "");
                    FinishLoading(_galleryState);
                    return;
                }
            }

            if (ct.IsCancellationRequested) {
                FinishLoading(_galleryState);
                return;
            }

            string serverTime = null;
            try {
                serverTime = await GetServerTime(_mw.httpClient, ct);
            }
            catch (HttpRequestException e) {
                _mw.AlertUser("An error has occurred while getting server time. Please try again.", e.Message);
            }
            if (serverTime == null) {
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

            await Task.WhenAll(tasks);

            int[] stillMissingIndexes = GetMissingIndexes(_mw.gallery);
            int[] loadedIndexes = missingIndexes.Except(stillMissingIndexes).ToArray();

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
                _mw.AlertUser("The following pages have failed to load. Try reducing thread number if the problem persists.", string.Join(", ", stillMissingIndexes));
            } else {
                _mw.AlertUser($"Reloading {_mw.gallery.id} has finished successfully", "");
            }

            bool isBookmarked = false;
            for (int i = 0; i < bmItems.Count; i++) {
                if (bmItems[i].gallery.id == _mw.gallery.id) {
                    bmItems[i].ReloadImages();
                    isBookmarked = true;
                    break;
                }
            }
            if (_mw.IsBookmarkFull()) {
                FinishLoading(GalleryState.BookmarkFull);
            }
            else if (isBookmarked) {
                FinishLoading(GalleryState.Bookmarked);
            }
            else {
                FinishLoading(GalleryState.Loaded);
            }
        }

        private CancellationTokenSource _autoScrollCts = new();
        public void StartStopAutoScroll(bool starting) {
            _isAutoScrolling = starting;
            AutoScrollBtn.IsChecked = starting;
            stopwatch.Reset();
            if (starting) {
                AutoScrollBtn.Icon = new SymbolIcon(Symbol.Pause);
                AutoScrollBtn.Label = "Stop Auto Page Turning / Scrolling";
                CancellationTokenSource cts = new();
                _autoScrollCts = cts;
                Task.Run(() => ScrollAutomatically(cts.Token), cts.Token);
            }
            else {
                _autoScrollCts.Cancel();
                _autoScrollCts = new();
                AutoScrollBtn.Icon = new SymbolIcon(Symbol.Play);
                AutoScrollBtn.Label = "Start Auto Page Turning / Scrolling";
            }
        }

        private void SetPageText() {
            PageNumText.Text = $"Page {_currPage} of {_mw.gallery.files.Length - 1}";
        }

        private void InsertSingleImage() {
            SetPageText();
            ImageContainer.Children.Clear();
            ImageContainer.Children.Add(_images[_currPage]);
        }

        private void InsertImages() {
            ImageContainer.Children.Clear();
            switch (_scrollDirection) {
                case ScrollDirection.TopToBottom or ScrollDirection.LeftToRight:
                    for (int i = 0; i < _images.Length; i++) {
                        ImageContainer.Children.Add(_images[i]);
                    }
                    break;
                case ScrollDirection.RightToLeft:
                    for (int i = _images.Length - 1; i >= 0; i--) {
                        ImageContainer.Children.Add(_images[i]);
                    }
                    break;
            }
        }

        private void HandleViewModeBtnClick(object _0, RoutedEventArgs _1) {
            StartStopAction(true);
            switch (_viewMode) {
                case ViewMode.Default:
                    _viewMode = ViewMode.Scroll;
                    ScrollDirectionSelector.IsEnabled = true;
                    ChangeScrollDirection(null, null);
                    break;
                case ViewMode.Scroll:
                    _viewMode = ViewMode.Default;
                    ScrollDirectionSelector.IsEnabled = false;
                    _currPage = GetPageFromScrollOffset();
                    InsertSingleImage();
                    MainScrollViewer.ScrollToVerticalOffset(0);
                    MainScrollViewer.ScrollToHorizontalOffset(0);
                    break;
            }
            SetScrollSpeedSlider();
            StartStopAction(false);
        }

        private async Task WaitImageLoad() {
            bool allLoaded = false;
            // wait for the images to be actually loaded into scrollview
            while (!allLoaded) {
                await Task.Delay(10);
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
            double pageHalfOffset = _scrollDirection switch {
                ScrollDirection.TopToBottom => MainScrollViewer.VerticalOffset + _mw.Bounds.Height / 2,
                ScrollDirection.LeftToRight => MainScrollViewer.HorizontalOffset + _mw.Bounds.Width / 2,
                ScrollDirection.RightToLeft => MainScrollViewer.ExtentWidth - (MainScrollViewer.HorizontalOffset + _mw.Bounds.Width / 2),
                _ => throw new ArgumentOutOfRangeException("_scrollDirection should be in range 0-2", "_scrollDirection")
            };
            double imageSizeSum = 0;
            switch (_scrollDirection) {
                case ScrollDirection.TopToBottom:
                    for (int i = 0; i < _images.Length; i++) {
                        imageSizeSum += _images[i].ActualHeight * MainScrollViewer.ZoomFactor;
                        if (imageSizeSum >= pageHalfOffset) {
                            return i;
                        }
                    }
                    break;
                case ScrollDirection.LeftToRight or ScrollDirection.RightToLeft:
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
                return _scrollDirection switch {
                    ScrollDirection.TopToBottom or ScrollDirection.LeftToRight => 0,
                    ScrollDirection.RightToLeft => MainScrollViewer.ScrollableWidth,
                    _ => throw new ArgumentOutOfRangeException("_scrollDirection should be in range 0-2", "_scrollDirection")
                };
            }
            double offset;
            switch (_scrollDirection) {
                case ScrollDirection.TopToBottom:
                    offset = 0;
                    for (int i = 0; i < _currPage; i++) {
                        offset += _images[i].ActualHeight * MainScrollViewer.ZoomFactor;
                    }
                    break;
                case ScrollDirection.LeftToRight:
                    offset = 0;
                    for (int i = 0; i < _currPage; i++) {
                        offset += _images[i].ActualWidth * MainScrollViewer.ZoomFactor;
                    }
                    break;
                case ScrollDirection.RightToLeft:
                    offset = MainScrollViewer.ScrollableWidth;
                    for (int i = _currPage - 1; i >= 0; i--) {
                        offset -= _images[i].ActualWidth * MainScrollViewer.ZoomFactor;
                    }
                    break;
                default:
                    throw new ArgumentException("_scrollDirection should be in range 0-2", "_scrollDirection");
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

        private static void IncrementPage(int num) {
            _pageMutex.WaitOne();
            _currPage = (_currPage + num + _mw.gallery.files.Length) % _mw.gallery.files.Length;
            _pageMutex.ReleaseMutex();
        }

        public void HandleKeyDown(object _, KeyRoutedEventArgs e) {
            switch (e.Key) {
                case VirtualKey.L:
                    LoopBtn.IsChecked = !LoopBtn.IsChecked;
                    break;
                case VirtualKey.Space:
                    if (!_isInAction && _galleryState != GalleryState.Empty) {
                        StartStopAutoScroll(!_isAutoScrolling);
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
                                IncrementPage(1);
                                InsertSingleImage();
                                break;
                            case VirtualKey.Left:
                                IncrementPage(-1);
                                InsertSingleImage();
                                break;
                        }
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
            while (_isAutoScrolling) {
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
                        if (_isAutoScrolling) {
                            if (_currPage + 1 == _mw.gallery.files.Length && !_isLooping) {
                                DispatcherQueue.TryEnqueue(() => StartStopAutoScroll(false));
                                return;
                            }
                            IncrementPage(1);
                            DispatcherQueue.TryEnqueue(InsertSingleImage);
                        }
                        break;
                    case ViewMode.Scroll:
                        try {
                            await Task.Delay(10, ct);
                        } catch (TaskCanceledException) {
                            return;
                        }
                        MainScrollViewer.DispatcherQueue.TryEnqueue(() => {
                            bool isEndOfPage = _scrollDirection switch {
                                ScrollDirection.TopToBottom => MainScrollViewer.VerticalOffset == MainScrollViewer.ScrollableHeight,
                                ScrollDirection.LeftToRight => MainScrollViewer.HorizontalOffset == MainScrollViewer.ScrollableWidth,
                                ScrollDirection.RightToLeft => MainScrollViewer.HorizontalOffset == 0,
                                _ => throw new ArgumentOutOfRangeException("_scrollDirection should be in range 0-2", "_scrollDirection")
                            };
                            if (isEndOfPage) {
                                if (_isLooping) {
                                    switch (_scrollDirection) {
                                        case ScrollDirection.TopToBottom:
                                            MainScrollViewer.ScrollToVerticalOffset(0);
                                            break;
                                        case ScrollDirection.LeftToRight:
                                            MainScrollViewer.ScrollToHorizontalOffset(0);
                                            break;
                                        case ScrollDirection.RightToLeft:
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
                                switch (_scrollDirection) {
                                    case ScrollDirection.TopToBottom:
                                        MainScrollViewer.ScrollToVerticalOffset(MainScrollViewer.VerticalOffset + _scrollSpeed * stopwatch.ElapsedMilliseconds);
                                        break;
                                    case ScrollDirection.LeftToRight:
                                        MainScrollViewer.ScrollToHorizontalOffset(MainScrollViewer.HorizontalOffset + _scrollSpeed * stopwatch.ElapsedMilliseconds);
                                        break;
                                    case ScrollDirection.RightToLeft:
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
                }
                _mw.sp.EnableControls(!start);
                EnableControls(!start);
                if (!start) {
                    if (_galleryState != GalleryState.Empty) {
                        LoadingControlBtn.Label = "Reload Gallery " + _mw.gallery.id;
                        LoadingControlBtn.Icon = new SymbolIcon(Symbol.Sync);
                        LoadingControlBtn.IsEnabled = true;
                    }
                    _isInAction = false;
                }
            }
        }

        public void EnableControls(bool enable) {
            if (!enable) {
                StartStopAutoScroll(false);
            }
            if (_galleryState != GalleryState.Empty) {
                ViewModeBtn.IsEnabled = enable;
                ScrollSpeedSlider.IsEnabled = enable;
                AutoScrollBtn.IsEnabled = enable;
            }
            DownloadThreadNumSelector.IsEnabled = enable;
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

        public async void LoadGalleryFromLocalDir(Gallery gallery) {
            StartLoading();
            // delete previous gallery if not bookmarked
            if (_mw.gallery != null) {
                if (_mw.GetGalleryFromBookmark(_mw.gallery.id) == null) {
                    DeleteGallery(_mw.gallery);
                }
            }
            _mw.gallery = gallery;
            _images = new Image[gallery.files.Length];
            LoadingProgressBar.Maximum = gallery.files.Length;
            string imageDir = Path.Combine(IMAGE_DIR, gallery.id);
            for (int i = 0; i < _images.Length; i++) {
                string[] file = Directory.GetFiles(imageDir, i.ToString() + ".*");
                _images[i] = new();
                if (file.Length > 0) {
                    _images[i].Source = new BitmapImage(new(file[0]));
                }
                LoadingProgressBar.Value++;
            }
            switch (_viewMode) {
                case ViewMode.Default:
                    _currPage = 0;
                    InsertSingleImage();
                    break;
                case ViewMode.Scroll:
                    InsertImages();
                    await WaitImageLoad();
                    switch (_scrollDirection) {
                        case ScrollDirection.TopToBottom:
                            MainScrollViewer.DispatcherQueue.TryEnqueue(() => MainScrollViewer.ScrollToVerticalOffset(0));
                            break;
                        case ScrollDirection.LeftToRight:
                            MainScrollViewer.DispatcherQueue.TryEnqueue(() => MainScrollViewer.ScrollToHorizontalOffset(0));
                            break;
                        case ScrollDirection.RightToLeft:
                            MainScrollViewer.DispatcherQueue.TryEnqueue(() => MainScrollViewer.ScrollToHorizontalOffset(MainScrollViewer.ScrollableWidth));
                            break;
                    }
                    _currPage = 0;
                    SetPageText();
                    break;
            }
            FinishLoading(GalleryState.Bookmarked);
        }

        public async Task LoadGalleryFromWeb(string id) {
            StartLoading();

            // create new cts
            CancellationTokenSource localCts = new();
            CancellationToken ct = localCts.Token;
            _cts = localCts;

            string galleryInfo = await GetGalleryInfo(_mw.httpClient, id, ct);
            if (galleryInfo == null) {
                FinishLoading(GalleryState.Empty);
                return;
            }
            
            if (ct.IsCancellationRequested) {
                FinishLoading(GalleryState.Empty);
                return;
            }

            Gallery newGallery; 
            try {
                newGallery = JsonSerializer.Deserialize<Gallery>(galleryInfo, serializerOptions);
            } catch (JsonException e) {
                _mw.AlertUser("Error while reading gallery json file", e.Message);
                FinishLoading(GalleryState.Empty);
                return;
            }

            string serverTime = null;
            try {
                serverTime = await GetServerTime(_mw.httpClient, ct);
            }
            catch (HttpRequestException e) {
                _mw.AlertUser("An error has occurred while getting server time. Please try again.", e.Message);
            }
            if (serverTime == null) {
                FinishLoading(GalleryState.Empty);
                return;
            }

            string[] imgFormats = GetImageFormats(newGallery.files);
            string[] imgAddresses = GetImageAddresses(newGallery.files, imgFormats, serverTime);

            if (ct.IsCancellationRequested) {
                FinishLoading(GalleryState.Empty);
                return;
            }

            LoadingProgressBar.Maximum = newGallery.files.Length;

            Task[] tasks = DownloadImages(
                _mw.httpClient,
                newGallery.id,
                imgAddresses,
                imgFormats,
                Enumerable.Range(0, newGallery.files.Length).ToArray(),
                _downloadThreadNum,
                LoadingProgressBar,
                ct
            );

            await Task.WhenAll(tasks);

            if (ct.IsCancellationRequested) {
                DeleteGallery(newGallery);
                FinishLoading(GalleryState.Empty);
                return;
            }

            int[] missingIndexes = GetMissingIndexes(newGallery);
            int[] loadedIndexes = Enumerable.Range(0, newGallery.files.Length).Except(missingIndexes).ToArray();

            string imageDir = Path.Combine(IMAGE_DIR, newGallery.id);

            Image[] newImages = new Image[newGallery.files.Length];
            for (int i = 0; i < newImages.Length; i++) {
                newImages[i] = new Image();
            }

            for (int i = 0; i < loadedIndexes.Length; i++) {
                int idx = loadedIndexes[i];
                string[] file = Directory.GetFiles(imageDir, idx.ToString() + ".*");
                newImages[idx].Source = new BitmapImage(new(file[0]));
            }

            // disable left/right key input
            _pageMutex.WaitOne();
            // delete previous gallery if not bookmarked
            if (_mw.gallery != null) {
                if (_mw.GetGalleryFromBookmark(_mw.gallery.id) == null) {
                    DeleteGallery(_mw.gallery);
                }
            }
            _mw.gallery = newGallery;
            _images = newImages;
            switch (_viewMode) {
                case ViewMode.Default:
                    _currPage = 0;
                    InsertSingleImage();
                    break;
                case ViewMode.Scroll:
                    InsertImages();
                    await WaitImageLoad();
                    switch (_scrollDirection) {
                        case ScrollDirection.TopToBottom:
                            DispatcherQueue.TryEnqueue(() => MainScrollViewer.ScrollToVerticalOffset(0));
                            break;
                        case ScrollDirection.LeftToRight:
                            DispatcherQueue.TryEnqueue(() => MainScrollViewer.ScrollToHorizontalOffset(0));
                            break;
                        case ScrollDirection.RightToLeft:
                            DispatcherQueue.TryEnqueue(() => MainScrollViewer.ScrollToHorizontalOffset(MainScrollViewer.ScrollableWidth));
                            break;
                    }
                    _currPage = 0;
                    SetPageText();
                    break;
            }
            _pageMutex.ReleaseMutex();

            if (missingIndexes.Length > 0) {
                _mw.AlertUser("The following pages have failed to load. Try reducing thread number if the problem persists.", string.Join(", ", missingIndexes));
            } else {
                _mw.AlertUser($"Gallery {newGallery.id} has loaded successfully", "");
            }

            if (_mw.IsBookmarkFull()) {
                FinishLoading(GalleryState.BookmarkFull);
            } else {
                FinishLoading(GalleryState.Loaded);
            }
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
                    bookmarkSignal.Set();
                    BookmarkBtn.Label = "Bookmarked";
                    break;
                case GalleryState.Bookmarking:
                    bookmarkSignal.Reset();
                    BookmarkBtn.Label = "Bookmarking...";
                    break;
                case GalleryState.BookmarkFull:
                    BookmarkBtn.Label = "Bookmark is full";
                    break;
                case GalleryState.Loaded:
                    BookmarkBtn.Label = "Bookmark this gallery";
                    break;
                case GalleryState.Empty:
                    BookmarkBtn.Label = "";
                    break;
            }
        }

        public async void HandleWindowClose() {
            bookmarkSignal.Wait();
            bookmarkSignal.Dispose();
            lock (_actionLock) if (_isInAction) _cts.Cancel();
            while (_isInAction) {
                await Task.Delay(10);
            }
            if (_mw.gallery != null) {
                if (_mw.GetGalleryFromBookmark(_mw.gallery.id) == null) {
                    DeleteGallery(_mw.gallery);
                }
            }
        }
    }
}
