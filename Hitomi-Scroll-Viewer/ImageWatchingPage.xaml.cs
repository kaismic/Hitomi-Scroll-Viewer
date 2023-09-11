using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using static Hitomi_Scroll_Viewer.MainWindow;
using static Hitomi_Scroll_Viewer.SearchPage;

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
        private double _imageScale = 0.5;

        private static int _currPage = 0;
        public Image[] images;

        private enum ViewMode {
            Default,
            Scroll
        }
        private static ViewMode _viewMode = ViewMode.Default;

        private readonly HttpClient _httpClient;
        private readonly int MAX_CONCURRENT_REQUEST = 4;
        private int _currMaxCncrReq = 1;

        private static readonly string GALLERY_INFO_DOMAIN = "https://ltn.hitomi.la/galleries/";
        private static readonly string GALLERY_INFO_EXCLUDE_STRING = "var galleryinfo = ";
        private static readonly string SERVER_TIME_ADDRESS = "https://ltn.hitomi.la/gg.js";
        private static readonly string SERVER_TIME_EXCLUDE_STRING = "0123456789/'\r\n};";
        private static readonly string REFERER = "https://hitomi.la/";
        private static readonly string[] POSSIBLE_IMAGE_SUBDOMAINS = { "https://aa.", "https://ba." };
        private static readonly JsonSerializerOptions serializerOptions = new() { IncludeFields = true };

        public enum GalleryState {
            Bookmarked,
            Bookmarking,
            BookmarkFull,
            Loaded,
            Empty
        }

        private static readonly Mutex _pageMutex = new();
        private readonly ManualResetEventSlim bookmarkSignal = new(true);

        // galleries for testing
        // https://hitomi.la/doujinshi/kameki-%E6%97%A5%E6%9C%AC%E8%AA%9E-2561144.html#1
        // https://hitomi.la/doujinshi/radiata-%E6%97%A5%E6%9C%AC%E8%AA%9E-2472850.html#1

        public ImageWatchingPage(MainWindow mainWindow) {
            _mw = mainWindow;

            InitializeComponent();

            SetScrollSpeedSlider();

            // Set ImageContainer top margin based on top commandbar height
            TopCommandBar.SizeChanged += (_0, _1) => {
                ImageContainer.Margin = new Thickness(0, TopCommandBar.ActualHeight, 0, 0);
            };

            // handle mouse movement on commandbar
            void handlePointerEnter(object _0, PointerRoutedEventArgs _1) {
                TopCommandBar.IsOpen = true;
                PageNumDisplay.Visibility = Visibility.Visible;
            }
            TopCommandBar.PointerEntered += handlePointerEnter;

            void handlePointerMove(object cb, PointerRoutedEventArgs args) {
                CommandBar commandBar = (CommandBar)cb;
                Point pos = args.GetCurrentPoint(MainGrid).Position;
                double center = MainGrid.ActualWidth / 2;
                double cbHalfWidth = commandBar.ActualWidth / 2;
                // commandBar.ActualHeight is the height at its ClosedDisplayMode
                // * 3 is the height of the commandbar when it is open and ClosedDisplayMode="Minimal"
                if (pos.Y > commandBar.ActualHeight * 3 || pos.X < center - cbHalfWidth || pos.X > center + cbHalfWidth) {
                    commandBar.IsOpen = false;
                    PageNumDisplay.Visibility = Visibility.Collapsed;
                }
            }
            TopCommandBar.PointerMoved += handlePointerMove;

            // Max concurrent request selector
            for (int i = 1; i <= MAX_CONCURRENT_REQUEST; i++) {
                MaxCncrRequestSelector.Items.Add(i);
            }
            MaxCncrRequestSelector.SelectedIndex = 0;
            MaxCncrRequestSelector.SelectionChanged += (_0, _1) => {
                _currMaxCncrReq = (int)MaxCncrRequestSelector.SelectedItem;
            };

            // set max connection per server for http client
            SocketsHttpHandler shh = new() {
                MaxConnectionsPerServer = MAX_CONCURRENT_REQUEST,
            };
            _httpClient = new(shh) {
                DefaultRequestHeaders = {
                    {"referer", REFERER }
                }
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

        private void HandleScrollViewChange(object _0, ScrollViewerViewChangingEventArgs _1) {
            if (_viewMode == ViewMode.Scroll) {
                GetPageFromScrollOffset();
                PageNumDisplay.Text = $"Page {_currPage} of {_mw.gallery.files.Length - 1}";
            }
        }

        private void HandleGoBackBtnClick(object _0, RoutedEventArgs _1) {
            _mw.SwitchPage();
        }

        private void HandleAutoScrollBtnClick(object _0, RoutedEventArgs _1) {
            StartStopAutoScroll(!_isAutoScrolling);
        }

        private void HandleLoadingControlBtnClick(object _0, RoutedEventArgs _1) {
            if (_mw.isInAction) {
                LoadingControlBtn.IsEnabled = false;
                _mw.cts.Cancel();
            } else {
                // reload gallery
                if (_mw.gallery != null) {
                    ReloadGallery();
                }
            }
        }

        // TODO
        // horizontal and vertical image scrolling in any 4 direction

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
            _mw.cts = localCts;

            string imageDir = IMAGE_DIR + DIR_SEP + _mw.gallery.id + DIR_SEP;

            Directory.CreateDirectory(imageDir);

            int[] missingIndexes = new int[_mw.gallery.files.Length];
            int missingCount = 0;

            if (reloadAll) {
                missingCount = _mw.gallery.files.Length;
                for (int i = 0; i < missingIndexes.Length; i++) {
                    missingIndexes[i] = i;
                }
            } else {
                for (int i = 0; i < missingIndexes.Length; i++) {
                    string path = imageDir + i + IMAGE_EXT;
                    if (!File.Exists(path)) {
                        missingIndexes[missingCount] = i;
                        missingCount++;
                    }
                }
                if (missingCount == 0) {
                    _mw.AlertUser("There are no missing images", "");
                    FinishLoading(_mw.galleryState);
                    return;
                }
            }

            if (ct.IsCancellationRequested) {
                FinishLoading(_mw.galleryState);
                return;
            }

            LoadingProgressBar.Maximum = missingCount;

            string[] imgHashArr = new string[missingCount];
            for (int i = 0; i < missingCount; i++) {
                int idx = missingIndexes[i];
                imgHashArr[i] = _mw.gallery.files[idx].hash;
            }

            string serverTime = await GetServerTime(ct);
            string[] imgAddresses;
            if (serverTime == null) {
                FinishLoading(_mw.galleryState);
                return;
            }
            imgAddresses = GetImageAddresses(imgHashArr, serverTime);

            if (ct.IsCancellationRequested) {
                FinishLoading(_mw.galleryState);
                return;
            }

            // example:
            // images length = 47, _currMaxCncrReq = 3
            // 47 / 3 = 15 r 2
            // 15+1 | 15+1 | 15
            int quotient = imgAddresses.Length / _currMaxCncrReq;
            int remainder = imgAddresses.Length % _currMaxCncrReq;
            Task[] tasks = new Task[_currMaxCncrReq];

            int startIdx = 0;
            for (int i = 0; i < _currMaxCncrReq; i++) {
                int thisStartIdx = startIdx;
                int thisJMax = quotient + (i < remainder ? 1 : 0);
                tasks[i] = Task.Run(async () => {
                    for (int j = 0; j < thisJMax; j++) {
                        if (ct.IsCancellationRequested) {
                            return;
                        }
                        int idx = thisStartIdx + j;
                        await TryGetImageBytesFromWeb(_mw.gallery.id, imgAddresses[idx], missingIndexes[idx], ct);
                    }
                });
                startIdx += thisJMax;
            }

            await Task.WhenAll(tasks);

            string missingIndexesText = "";
            for (int i = 0; i < missingCount; i++) {
                int idx = missingIndexes[i];
                if (ct.IsCancellationRequested) {
                    FinishLoading(_mw.galleryState);
                    return;
                }
                string path = imageDir + idx + IMAGE_EXT;
                if (File.Exists(path)) {
                    images[idx].Source = new BitmapImage(new(path));
                } else {
                    missingIndexesText += idx + ", ";
                }
            }

            // disable left/right key input
            _pageMutex.WaitOne();
            switch (_viewMode) {
                case ViewMode.Default:
                    InsertSingleImage();
                    break;
                case ViewMode.Scroll:
                    InsertImages();
                    await WaitImageLoad();
                    DispatcherQueue.TryEnqueue(() => MainScrollViewer.ScrollToVerticalOffset(GetScrollOffsetFromPage()));
                    break;
            }
            _pageMutex.ReleaseMutex();

            if (missingIndexesText != "") {
                _mw.AlertUser("The image at the following pages have failed to load. Try reducing max concurrent request if the problem persists.", missingIndexesText[..^2]);
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
                Task.Run(() => ScrollAutomatically(cts.Token));
            }
            else {
                _autoScrollCts.Cancel();
                _autoScrollCts = new();
                AutoScrollBtn.Icon = new SymbolIcon(Symbol.Play);
                AutoScrollBtn.Label = "Start Auto Page Turning / Scrolling";
            }
        }

        private void InsertSingleImage() {
            PageNumDisplay.Text = $"Page {_currPage} of {_mw.gallery.files.Length - 1}";
            ImageContainer.Children.Clear();
            ImageContainer.Children.Add(images[_currPage]);
        }

        private void InsertImages() {
            ImageContainer.Children.Clear();
            for (int i = 0; i < images.Length; i++) {
                ImageContainer.Children.Add(images[i]);
            }
        }

        private async void HandleViewModeBtnClick(object _0, RoutedEventArgs _1) {
            _mw.StartStopAction(true);
            switch (_viewMode) {
                case ViewMode.Default:
                    _viewMode = ViewMode.Scroll;
                    InsertImages();
                    await WaitImageLoad();
                    DispatcherQueue.TryEnqueue(() => MainScrollViewer.ScrollToVerticalOffset(GetScrollOffsetFromPage()));
                    break;
                case ViewMode.Scroll:
                    _viewMode = ViewMode.Default;
                    GetPageFromScrollOffset();
                    InsertSingleImage();
                    MainScrollViewer.ScrollToVerticalOffset(0);
                    break;
            }
            SetScrollSpeedSlider();
            _mw.StartStopAction(false);
        }

        private async Task WaitImageLoad() {
            bool allLoaded = false;
            // wait for the images to be actually loaded into scrollview
            while (!allLoaded) {
                await Task.Delay(10);
                allLoaded = true;
                for (int i = 0; i < images.Length; i++) {
                    if (!images[i].IsLoaded) {
                        allLoaded = false;
                        break;
                    }
                }
            }
        }

        private void GetPageFromScrollOffset() {
            double currOffset = MainScrollViewer.VerticalOffset;
            double imageHeightSum = ImageContainer.Margin.Top;
            if (currOffset < imageHeightSum) {
                _currPage = 0;
                return;
            }
            // half of the window height is the reference height for page calculation
            double pageHalfOffset = currOffset + _mw.appWindow.ClientSize.Height / 2;
            for (int i = 0; i < images.Length; i++) {
                imageHeightSum += images[i].ActualHeight;
                if (imageHeightSum >= pageHalfOffset) {
                    _currPage = i;
                    return;
                }
            }
        }

        private double GetScrollOffsetFromPage() {
            if (_currPage == 0) return 0;
            double offset = ImageContainer.Margin.Top;
            for (int i = 0; i < images.Length; i++) {
                if (i >= _currPage) {
                    return offset;
                }
                offset += images[i].ActualHeight;
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
            if (e.Key == Windows.System.VirtualKey.L) {
                LoopBtn.IsChecked = !LoopBtn.IsChecked;
            }
            if (!_mw.isInAction) {
                if (e.Key == Windows.System.VirtualKey.Space && _mw.galleryState != GalleryState.Empty) {
                    StartStopAutoScroll(!_isAutoScrolling);
                }
                if (_mw.gallery != null) {
                    if (_viewMode == ViewMode.Default) {
                        _pageMutex.WaitOne();
                        if (e.Key == Windows.System.VirtualKey.Right) {
                            IncrementPage(1);
                            InsertSingleImage();
                        }
                        else if (e.Key == Windows.System.VirtualKey.Left) {
                            IncrementPage(-1);
                            InsertSingleImage();
                        }
                        _pageMutex.ReleaseMutex();
                    }
                }
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
                        }
                        catch (TaskCanceledException) {
                            return;
                        }
                        DispatcherQueue.TryEnqueue(() => {
                            if (MainScrollViewer.VerticalOffset != MainScrollViewer.ScrollableHeight) {
                                stopwatch.Stop();
                                MainScrollViewer.ScrollToVerticalOffset(MainScrollViewer.VerticalOffset + _scrollSpeed * stopwatch.ElapsedMilliseconds);
                                stopwatch.Restart();
                            }
                            else {
                                if (_isLooping) {
                                    MainScrollViewer.ScrollToVerticalOffset(0);
                                }
                                else {
                                    StartStopAutoScroll(false);
                                    return;
                                }
                            }
                        });
                        break;
                }
            }
        }

        /**
         * <summary>
         * Change image size and re-position vertical offset to the page that the user was already at.
         * </summary>
         */
        private async void ChangeImageSize(object sender, RangeBaseValueChangedEventArgs _1) {
            ((Slider)sender).IsEnabled = false;
            Slider slider = (Slider)sender;
            _imageScale = slider.Value;
            if (ImageContainer != null && _mw.gallery != null) {
                if (_viewMode == ViewMode.Scroll) {
                    GetPageFromScrollOffset();
                }
                for (int i = 0; i < images.Length; i++) {
                    images[i].Width = _mw.gallery.files[i].width * _imageScale;
                    images[i].Height = _mw.gallery.files[i].height * _imageScale;
                }
                if (_viewMode == ViewMode.Scroll) {
                    // wait for the actual image heights to be updated
                    double PIXEL_MARGIN = 1;
                    bool heightAllSet = false;
                    int startIdx = 0;
                    while (!heightAllSet) {
                        heightAllSet = true;
                        for (int i = startIdx; i < images.Length; i++) {
                            // if actual height is not within the expected height range 
                            if (images[i].ActualHeight < images[i].Height - PIXEL_MARGIN
                                || images[i].ActualHeight > images[i].Height + PIXEL_MARGIN) {
                                startIdx = i;
                                heightAllSet = false;
                                await Task.Delay(10);
                                break;
                            }
                        }
                    }
                    // set vertical offset according to the new image scale
                    MainScrollViewer.ScrollToVerticalOffset(GetScrollOffsetFromPage());
                }
            }
            slider.IsEnabled = true;
        }

        public void EnableControls(bool enable) {
            if (!enable) {
                StartStopAutoScroll(false);
            }
            if (_mw.galleryState != GalleryState.Empty) {
                ViewModeBtn.IsEnabled = enable;
                ImageScaleSlider.IsEnabled = enable;
                ScrollSpeedSlider.IsEnabled = enable;
                AutoScrollBtn.IsEnabled = enable;
            }
            MaxCncrRequestSelector.IsEnabled = enable;
        }

        private void StartLoading() {
            _mw.StartStopAction(true);
            LoadingProgressBar.Visibility = Visibility.Visible;
            LoadingProgressBar.Value = 0;
        }

        private void FinishLoading(GalleryState state) {
            LoadingProgressBar.Visibility = Visibility.Collapsed;
            ChangeBookmarkBtnState(state);
            _httpClient.CancelPendingRequests();
            _mw.StartStopAction(false);
        }

        public void LoadGalleryFromLocalDir(Gallery gallery) {
            StartLoading();
            // delete previous gallery if not bookmarked
            if (_mw.gallery != null) {
                if (_mw.GetGalleryFromBookmark(_mw.gallery.id) == null) {
                    DeleteGallery(_mw.gallery);
                }
            }
            _mw.gallery = gallery;
            images = new Image[gallery.files.Length];
            LoadingProgressBar.Maximum = gallery.files.Length;
            string dir = IMAGE_DIR + DIR_SEP + gallery.id + DIR_SEP;
            for (int i = 0; i < images.Length; i++) {
                string path = dir + i + IMAGE_EXT;
                images[i] = new() {
                    Width = gallery.files[i].width * _imageScale,
                    Height = gallery.files[i].height * _imageScale
                };
                if (File.Exists(path)) {
                    images[i].Source = new BitmapImage(new(path));
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
                    break;
            }
            FinishLoading(GalleryState.Bookmarked);
        }

        private async Task<string> GetGalleryInfo(string id, CancellationToken ct) {
            string address = GALLERY_INFO_DOMAIN + id + ".js";
            HttpRequestMessage galleryInfoRequest = new() {
                Method = HttpMethod.Get,
                RequestUri = new Uri(address)
            };
            HttpResponseMessage response;
            try {
                response = await _httpClient.SendAsync(galleryInfoRequest, ct);
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException ex) {
                _mw.AlertUser("An error has occurred while getting gallery info. Please try again.", ex.Message);
                return null;
            }
            catch (TaskCanceledException) when (ct.IsCancellationRequested) {
                return null;
            }
            string responseString = await response.Content.ReadAsStringAsync(ct);
            return responseString[GALLERY_INFO_EXCLUDE_STRING.Length..];
        }

        private async Task<string> GetServerTime(CancellationToken ct) {
            HttpRequestMessage serverTimeRequest = new() {
                Method = HttpMethod.Get,
                RequestUri = new Uri(SERVER_TIME_ADDRESS)
            };
            HttpResponseMessage response;
            try {
                response = await _httpClient.SendAsync(serverTimeRequest, ct);
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException ex) {
                _mw.AlertUser("An error has occurred while getting server time. Please try again.", ex.Message);
                return null;
            }
            catch (TaskCanceledException) when (ct.IsCancellationRequested) {
                return null;
            }
            string responseString = await response.Content.ReadAsStringAsync(ct);

            // get numbers between ' and /'
            return responseString.Substring(responseString.Length - SERVER_TIME_EXCLUDE_STRING.Length, 10);
        }

        private static string[] GetImageAddresses(string[] imgHashArr, string serverTime) {
            string[] result = new string[imgHashArr.Length];
            for (int i = 0; i < imgHashArr.Length; i++) {
                string hash = imgHashArr[i];
                string oneTwoCharInt = Convert.ToInt32(hash[^1..] + hash[^3..^1], 16).ToString();
                result[i] = $"hitomi.la/webp/{serverTime}/{oneTwoCharInt}/{hash}.webp";
            }
            return result;
        }

        /**
         * <returns>The image <c>byte[]</c> if the given address is valid, otherwise <c>null</c>.</returns>
         */
        public async Task<byte[]> GetImageBytesFromWeb(string address, CancellationToken ct) {
            HttpRequestMessage request = new() {
                Method = HttpMethod.Get,
                RequestUri = new Uri(address),
            };
            HttpResponseMessage response;
            if (ct.IsCancellationRequested) {
                return null;
            }
            try {
                response = await _httpClient.SendAsync(request, ct);
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException e) {
                if (e.StatusCode != HttpStatusCode.NotFound) {
                    Debug.WriteLine(e.Message);
                    Debug.WriteLine("Status Code: " + e.StatusCode);
                }
                return null;
            }
            catch (TaskCanceledException) when (ct.IsCancellationRequested) {
                return null;
            }
            if (ct.IsCancellationRequested) {
                return null;
            }
            return await response.Content.ReadAsByteArrayAsync(ct);
        }

        private async Task TryGetImageBytesFromWeb(string id, string imgAddress, int idx, CancellationToken ct) {
            foreach (string subdomain in POSSIBLE_IMAGE_SUBDOMAINS) {
                if (ct.IsCancellationRequested) {
                    return;
                }
                byte[] imageBytes = await GetImageBytesFromWeb(subdomain + imgAddress, ct);
                if (imageBytes != null) {
                    try {
                        await File.WriteAllBytesAsync(IMAGE_DIR + DIR_SEP + id + DIR_SEP + idx + IMAGE_EXT, imageBytes, ct);
                    } catch (IOException) {
                        return;
                    }
                    break;
                }
            }
            DispatcherQueue.TryEnqueue(() => {
                LoadingProgressBar.Value++;
            });
        }

        public async Task LoadGalleryFromWeb(string id) {
            StartLoading();

            // create new cts
            CancellationTokenSource localCts = new();
            CancellationToken ct = localCts.Token;
            _mw.cts = localCts;

            string galleryInfo = await GetGalleryInfo(id, ct);
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

            LoadingProgressBar.Maximum = newGallery.files.Length;

            string[] imgHashArr = new string[newGallery.files.Length];
            for (int i = 0; i < newGallery.files.Length; i++) {
                imgHashArr[i] = newGallery.files[i].hash;
            }

            string serverTime = await GetServerTime(ct);
            string[] imgAddresses;
            if (serverTime == null) {
                FinishLoading(GalleryState.Empty);
                return;
            }
            imgAddresses = GetImageAddresses(imgHashArr, serverTime);

            if (ct.IsCancellationRequested) {
                FinishLoading(GalleryState.Empty);
                return;
            }

            Directory.CreateDirectory(IMAGE_DIR + DIR_SEP + id);
            // example:
            // images length = 47, _currMaxCncrReq = 3
            // 47 / 3 = 15 r 2
            // 15+1 | 15+1 | 15
            int quotient = imgAddresses.Length / _currMaxCncrReq;
            int remainder = imgAddresses.Length % _currMaxCncrReq;
            Task[] tasks = new Task[_currMaxCncrReq];

            int startIdx = 0;
            for (int i = 0; i < _currMaxCncrReq; i++) {
                int thisStartIdx = startIdx;
                int thisJMax = quotient + (i < remainder ? 1 : 0);
                tasks[i] = Task.Run(async () => {
                    for (int j = 0; j < thisJMax; j++) {
                        if (ct.IsCancellationRequested) {
                            return;
                        }
                        int idx = thisStartIdx + j;
                        await TryGetImageBytesFromWeb(newGallery.id, imgAddresses[idx], idx, ct);
                    }
                });
                startIdx += thisJMax;
            }

            await Task.WhenAll(tasks);

            if (ct.IsCancellationRequested) {
                FinishLoading(GalleryState.Empty);
                return;
            }

            string imageDir = IMAGE_DIR + DIR_SEP + newGallery.id + DIR_SEP;
            Image[] newImages = new Image[newGallery.files.Length];
            string missingIndexesText = "";
            for (int i = 0; i < newGallery.files.Length; i++) {
                if (ct.IsCancellationRequested) {
                    FinishLoading(GalleryState.Empty);
                    return;
                }
                string path = imageDir + i + IMAGE_EXT;
                newImages[i] = new() {
                    Width = newGallery.files[i].width * _imageScale,
                    Height = newGallery.files[i].height * _imageScale
                };
                if (File.Exists(path)) {
                    newImages[i].Source = new BitmapImage(new(path));
                } else {
                    missingIndexesText += i + ", ";
                }
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
            images = newImages;
            _currPage = 0;
            switch (_viewMode) {
                case ViewMode.Default:
                    InsertSingleImage();
                    break;
                case ViewMode.Scroll:
                    InsertImages();
                    await WaitImageLoad();
                    DispatcherQueue.TryEnqueue(() => MainScrollViewer.ScrollToVerticalOffset(GetScrollOffsetFromPage()));
                    break;
            }
            _pageMutex.ReleaseMutex();

            if (missingIndexesText != "") {
                _mw.AlertUser("The image at the following pages have failed to load. Try reducing max concurrent request if the problem persists.", missingIndexesText[..^2]);
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
            _mw.galleryState = state;
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

        public void WaitBookmarking() {
            bookmarkSignal.Wait();
            bookmarkSignal.Dispose();
        }
    }
}
