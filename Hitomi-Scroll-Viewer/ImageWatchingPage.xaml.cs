using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using static Hitomi_Scroll_Viewer.MainWindow;

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
        private static double _pageTurnDelay = 2; // in seconds
        private bool _isAutoScrolling = false;
        private bool _isLooping = true;
        private double _imageScale = 0.5;

        private static int _currPage = 0;
        private static Image[] _images;

        private enum ViewMode {
            Default,
            Scroll
        }
        private static ViewMode _viewMode = ViewMode.Default;

        private readonly HttpClient _httpClient;

        private static readonly string GALLERY_INFO_DOMAIN = "https://ltn.hitomi.la/galleries/";
        private static readonly string GALLERY_INFO_EXCLUDE_STRING = "var galleryinfo = ";
        private static readonly string SERVER_TIME_ADDRESS = "https://ltn.hitomi.la/gg.js";
        private static readonly string SERVER_TIME_EXCLUDE_STRING = "9999999999/'\r\n};";
        private static readonly string REFERER = "https://hitomi.la/";
        private static readonly string[] POSSIBLE_IMAGE_SUBDOMAINS = { "https://aa.", "https://ba." };
        private static readonly JsonSerializerOptions serializerOptions = new() { IncludeFields = true };

        private static CancellationTokenSource _cts = new();
        private static CancellationToken _ct = _cts.Token;

        public enum GalleryState {
            Bookmarked,
            Bookmarking,
            BookmarkFull,
            Loaded,
            Loading,
            Empty
        }

        private static GalleryState _galleryState = GalleryState.Empty;
        private static bool _isInAction = false;

        private static int _loadRequestCounter = 0;

        // galleries for testing
        // https://hitomi.la/doujinshi/kameki-%E6%97%A5%E6%9C%AC%E8%AA%9E-2561144.html#1
        // https://hitomi.la/doujinshi/radiata-%E6%97%A5%E6%9C%AC%E8%AA%9E-2472850.html#1

        public ImageWatchingPage(MainWindow mainWindow) {
            InitializeComponent();

            DisableControls();
            SetScrollSpeedSlider();

            _mw = mainWindow;

            // Set ImageContainer top margin based on top commandbar height
            void handleSizeChange(object cb, SizeChangedEventArgs _1) {
                ImageContainer.Margin = new Thickness(0, ((CommandBar)cb).ActualHeight, 0, 0);
            }

            TopCommandBar.SizeChanged += handleSizeChange;

            // handle mouse movement on commandbar
            void handlePointerEnter(object cb, PointerRoutedEventArgs _1) {
                ((CommandBar)cb).IsOpen = true;
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

            // set max connection per server for http client
            SocketsHttpHandler shh = new() {
                MaxConnectionsPerServer = 1,
            };
            _httpClient = new(shh) {
                DefaultRequestHeaders = {
                    {"referer", REFERER }
                },
            };
        }

        private void SetScrollSpeedSlider() {
            switch (_viewMode) {
                case ViewMode.Default:
                    // save prev value because it SetScrollSpeed is called when min max is set
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
                    // save prev value because it SetScrollSpeed is called when min max is set
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
                PageNumDisplay.Text = $"Page {_currPage + 1} of {gallery.files.Count}";
            }
        }

        private void HandleGoBackBtnClick(object _0, RoutedEventArgs _1) {
            _mw.SwitchPage();
        }

        private void HandleAutoScrollBtnClick(object _0, RoutedEventArgs _1) {
            SetAutoScroll(_isAutoScrolling);
        }

        private async void HandleReloadBtnClick(object _0, RoutedEventArgs _1) {
            await LoadGalleryFromWeb(gallery.id);
        }

        public void SetAutoScroll(bool newValue) {
            AutoScrollBtn.IsChecked = newValue;
            stopwatch.Reset();
            if (_isAutoScrolling) {
                AutoScrollBtn.Icon = new SymbolIcon(Symbol.Pause);
                AutoScrollBtn.Label = "Stop Auto Page Turning / Scrolling";
                Task.Run(ScrollAutomatically);
            }
            else {
                AutoScrollBtn.Icon = new SymbolIcon(Symbol.Play);
                AutoScrollBtn.Label = "Start Auto Page Turning / Scrolling";
            }
        }

        private void InsertSingleImage() {
            PageNumDisplay.Text = $"Page {_currPage + 1} of {gallery.files.Count}";
            ImageContainer.Children.Clear();
            ImageContainer.Children.Add(_images[_currPage]);
        }

        private void InsertImages() {
            ImageContainer.Children.Clear();
            for (int i = 0; i < _images.Length; i++) {
                _ct.ThrowIfCancellationRequested();
                ImageContainer.Children.Add(_images[i]);
            }
        }

        private async void HandleViewModeBtnClick(object _0, RoutedEventArgs _1) {
            if (!await RequestActionPermit()) {
                return;
            }
            DisableControls();
            try {
                switch (_viewMode) {
                    case ViewMode.Default:
                        _viewMode = ViewMode.Scroll;
                        InsertImages();
                        bool allLoaded = false;
                        // wait for the images to be actually loaded into scrollview
                        while (!allLoaded) {
                            await Task.Delay(10);
                            allLoaded = true;
                            for (int i = 0; i < _images.Length; i++) {
                                if (!_images[i].IsLoaded) {
                                    allLoaded = false;
                                    break;
                                }
                            }
                        }
                        DispatcherQueue.TryEnqueue(() => MainScrollViewer.ScrollToVerticalOffset(GetScrollOffsetFromPage()));
                        break;
                    case ViewMode.Scroll:
                        _viewMode = ViewMode.Default;
                        GetPageFromScrollOffset();
                        InsertSingleImage();
                        break;
                }
                SetScrollSpeedSlider();
            }
            catch (OperationCanceledException) { }
            finally {
                StopAction();
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
            for (int i = 0; i < _images.Length; i++) {
                imageHeightSum += _images[i].ActualHeight;
                if (imageHeightSum >= pageHalfOffset) {
                    _currPage = i;
                    return;
                }
            }
        }

        private double GetScrollOffsetFromPage() {
            if (_currPage == 0) {
                return 0;
            }
            double offset = ImageContainer.Margin.Top;
            for (int i = 0; i < _images.Length; i++) {
                if (i >= _currPage) {
                    return offset;
                }
                offset += _images[i].ActualHeight;
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
            _currPage = (_currPage + num + gallery.files.Count) % gallery.files.Count;
        }

        public void HandleKeyDown(object _, KeyRoutedEventArgs e) {
            if (e.Key == Windows.System.VirtualKey.L) {
                LoopBtn.IsChecked = !LoopBtn.IsChecked;
            }
            if (gallery != null && !_isInAction) {
                _isInAction = true;
                if (_viewMode == ViewMode.Default) {
                    if (e.Key is Windows.System.VirtualKey.Right or Windows.System.VirtualKey.RightButton) {
                        IncrementPage(1);
                        InsertSingleImage();
                    }
                    else if (e.Key is Windows.System.VirtualKey.Left or Windows.System.VirtualKey.LeftButton) {
                        IncrementPage(-1);
                        InsertSingleImage();
                    }
                }
                if (e.Key == Windows.System.VirtualKey.Space) {
                    SetAutoScroll(!_isAutoScrolling);
                }
                _isInAction = false;
            }
        }

        // for updating auto scrolling in sync with real time
        private static readonly Stopwatch stopwatch = new();

        private async void ScrollAutomatically() {
            while (_isAutoScrolling) {
                switch (_viewMode) {
                    case ViewMode.Default:
                        if (_currPage + 1 == gallery.files.Count && !_isLooping) {
                            DispatcherQueue.TryEnqueue(() => SetAutoScroll(false));
                            return;
                        }
                        await Task.Delay((int)(_pageTurnDelay * 1000));
                        if (_isAutoScrolling) {
                            if (_currPage + 1 == gallery.files.Count && !_isLooping) {
                                DispatcherQueue.TryEnqueue(() => SetAutoScroll(false));
                                return;
                            }
                            IncrementPage(1);
                            DispatcherQueue.TryEnqueue(InsertSingleImage);
                        }
                        break;
                    case ViewMode.Scroll:
                        await Task.Delay(10);
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
                                    SetAutoScroll(false);
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
            if (ImageContainer != null && gallery != null) {
                if (_viewMode == ViewMode.Scroll) {
                    GetPageFromScrollOffset();
                }
                for (int i = 0; i < _images.Length; i++) {
                    _images[i].Width = gallery.files[i].width * _imageScale;
                    _images[i].Height = gallery.files[i].height * _imageScale;
                }
                if (_viewMode == ViewMode.Scroll) {
                    // wait for the actual image heights to be updated
                    double PIXEL_MARGIN = 1;
                    bool heightAllSet = false;
                    int startIdx = 0;
                    while (!heightAllSet) {
                        heightAllSet = true;
                        for (int i = startIdx; i < _images.Length; i++) {
                            Debug.WriteLine(i + " Height = " + _images[i].Height + " Actual Height = " + _images[i].ActualHeight);
                            // if actual height is not within the expected height range 
                            if (_images[i].ActualHeight < _images[i].Height - PIXEL_MARGIN
                                || _images[i].ActualHeight > _images[i].Height + PIXEL_MARGIN) {
                                Debug.WriteLine("called at i = " + i + " _imageScale = " + _imageScale);
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

        private void DisableControls() {
            SetAutoScroll(false);
            ViewModeBtn.IsEnabled = false;
            ImageScaleSlider.IsEnabled = false;
            AutoScrollBtn.IsEnabled = false;
            ReloadBtn.IsEnabled = false;
        }

        private void StopAction() {
            ViewModeBtn.IsEnabled = true;
            ImageScaleSlider.IsEnabled = true;
            AutoScrollBtn.IsEnabled = true;
            ReloadBtn.IsEnabled = true;
            _isInAction = false;
        }

        /**
         * <returns><c>true</c> if the load request is permitted, otherwise <c>false</c></returns>
         */
        private static async Task<bool> RequestActionPermit() {
            if (_isInAction) {
                int rank = Interlocked.Increment(ref _loadRequestCounter);
                if (rank == 1) {
                    // first load request so send cancel request
                    _cts.Cancel();
                }
                while (_isInAction) {
                    await Task.Delay(10);
                    // this request is not the latest request anymore
                    if (_loadRequestCounter != rank) {
                        return false;
                    }
                }
                // loading finished
                // this request is the latest request
                _isInAction = true;
                _loadRequestCounter = 0;
                _cts.Dispose();
                _cts = new();
                _ct = _cts.Token;
                return true;
            }
            _isInAction = true;
            return true;
        }

        private async Task<bool> StartLoading() {
            if (!await RequestActionPermit()) {
                return false;
            }

            DisableControls();

            ChangeBookmarkBtnState(GalleryState.Loading);

            // check if we have a gallery already loaded
            if (gallery != null) {
                // if the loaded gallery is not bookmarked delete it from local directory
                if (!IsBookmarked()) {
                    DeleteGallery(gallery);
                }
            }
            return true;
        }

        private void FinishLoading(GalleryState state) {
            ChangeBookmarkBtnState(state);
            StopAction();
        }

        public async Task LoadGalleryFromLocalDir(Gallery newGallery) {
            if (!await StartLoading()) {
                return;
            }
            gallery = newGallery;
            _images = new Image[gallery.files.Count];
            string dir = IMAGE_DIR + @"\" + gallery.id + @"\";
            for (int i = 0; i < _images.Length; i++) {
                _ct.ThrowIfCancellationRequested();
                _images[i] = new() {
                    Source = new BitmapImage(new(dir + i + IMAGE_EXT)),
                    Width = gallery.files[i].width * _imageScale,
                    Height = gallery.files[i].height * _imageScale
                };
            }
            try {
                switch (_viewMode) {
                    case ViewMode.Default:
                        _ct.ThrowIfCancellationRequested();
                        _currPage = 0;
                        InsertSingleImage();
                        break;
                    case ViewMode.Scroll:
                        _ct.ThrowIfCancellationRequested();
                        InsertImages();
                        break;
                }
            }
            catch (OperationCanceledException) { }
            finally {
                if (_ct.IsCancellationRequested) {
                    FinishLoading(GalleryState.Loading);
                }
                else {
                    FinishLoading(GalleryState.Bookmarked);
                }
            }
        }

        private async Task<string> GetGalleryInfo(string id) {
            string address = GALLERY_INFO_DOMAIN + id + ".js";
            HttpRequestMessage galleryInfoRequest = new() {
                Method = HttpMethod.Get,
                RequestUri = new Uri(address)
            };
            HttpResponseMessage response;
            try {
                response = await _httpClient.SendAsync(galleryInfoRequest, _ct);
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException ex) {
                _mw.AlertUser("An error has occurred while getting gallery info. Please try again.", ex.Message);
                return null;
            }
            catch (TaskCanceledException) {
                return null;
            }
            string responseString = await response.Content.ReadAsStringAsync();
            return responseString[GALLERY_INFO_EXCLUDE_STRING.Length..];
        }

        private async Task<string> GetServerTime() {
            HttpRequestMessage serverTimeRequest = new() {
                Method = HttpMethod.Get,
                RequestUri = new Uri(SERVER_TIME_ADDRESS)
            };
            HttpResponseMessage response;
            try {
                response = await _httpClient.SendAsync(serverTimeRequest, _ct);
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException ex) {
                _mw.AlertUser("An error has occurred while getting server time. Please try again.", ex.Message);
                return null;
            }
            catch (TaskCanceledException) {
                return null;
            }
            string responseString = await response.Content.ReadAsStringAsync();

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
        public async Task<byte[]> GetImageBytesFromWeb(string address) {
            HttpRequestMessage request = new() {
                Method = HttpMethod.Get,
                RequestUri = new Uri(address),
            };
            HttpResponseMessage response;
            try {
                response = await _httpClient.SendAsync(request, _ct);
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException e) {
                if (e.StatusCode != System.Net.HttpStatusCode.NotFound) {
                    Debug.WriteLine(e.Message);
                    Debug.WriteLine("Status Code: " + e.StatusCode);
                }
                return null;
            }
            catch (TaskCanceledException) {
                return null;
            }
            return await response.Content.ReadAsByteArrayAsync();
        }

        private async Task TryGetImageBytesFromWeb(string imgAddress, int idx) {
            byte[] imageBytes;
            _ct.ThrowIfCancellationRequested();
            foreach (string subdomain in POSSIBLE_IMAGE_SUBDOMAINS) {
                imageBytes = await GetImageBytesFromWeb(subdomain + imgAddress);
                if (imageBytes != null) {
                    await SaveImage(gallery.id, idx, imageBytes, _ct);
                    LoadingProgressBar.Value++;
                }
            }
        }

        public async Task LoadGalleryFromWeb(string id) {
            if (!await StartLoading()) {
                return;
            }
            try {
                _ct.ThrowIfCancellationRequested();

                string galleryInfo = await GetGalleryInfo(id);

                if (galleryInfo == null) {
                    FinishLoading(GalleryState.Empty);
                    return;
                }
                gallery = JsonSerializer.Deserialize<Gallery>(galleryInfo, serializerOptions);

                // show LoadingProgressBar
                LoadingProgressBar.Value = 0;
                LoadingProgressBar.Maximum = gallery.files.Count;
                LoadingProgressBar.Visibility = Visibility.Visible;

                _ct.ThrowIfCancellationRequested();

                string[] imgHashArr = new string[gallery.files.Count];
                for (int i = 0; i < gallery.files.Count; i++) {
                    imgHashArr[i] = gallery.files[i].hash;
                }

                string serverTime = await GetServerTime();
                string[] imgAddresses;
                if (serverTime == null) {
                    FinishLoading(GalleryState.Empty);
                    return;
                }
                imgAddresses = GetImageAddresses(imgHashArr, serverTime);
                _ct.ThrowIfCancellationRequested();

                Directory.CreateDirectory(IMAGE_DIR + @"\" + id);

                Task[] tasks = new Task[imgAddresses.Length];

                for (int i = 0; i < imgAddresses.Length; i++) {
                    _ct.ThrowIfCancellationRequested();
                    tasks[i] = TryGetImageBytesFromWeb(imgAddresses[i], i);
                }

                await Task.WhenAll(tasks);

                _images = new Image[gallery.files.Count];
                string dir = IMAGE_DIR + @"\" + gallery.id + @"\";
                for (int i = 0; i < gallery.files.Count; i++) {
                    _ct.ThrowIfCancellationRequested();
                    _images[i] = new() {
                        Source = new BitmapImage(new(dir + i + IMAGE_EXT)),
                        Width = gallery.files[i].width * _imageScale,
                        Height = gallery.files[i].height * _imageScale
                    };
                }

                switch (_viewMode) {
                    case ViewMode.Default:
                        _ct.ThrowIfCancellationRequested();
                        _currPage = 0;
                        InsertSingleImage();
                        break;
                    case ViewMode.Scroll:
                        _ct.ThrowIfCancellationRequested();
                        InsertImages();
                        break;
                }
            }
            catch (OperationCanceledException) { }
            finally {
                if (_ct.IsCancellationRequested) {
                    FinishLoading(GalleryState.Loading);
                }
                else if (IsBookmarkFull()) {
                    FinishLoading(GalleryState.BookmarkFull);
                }
                else if (IsBookmarked()) {
                    FinishLoading(GalleryState.Bookmarked);
                }
                else {
                    FinishLoading(GalleryState.Loaded);
                }
                // hide LoadingProgressBar
                LoadingProgressBar.Visibility = Visibility.Collapsed;
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
                    BookmarkBtn.Label = "Bookmarked";
                    break;
                case GalleryState.Bookmarking:
                    BookmarkBtn.Label = "Bookmarking...";
                    break;
                case GalleryState.BookmarkFull:
                    BookmarkBtn.Label = "Bookmark is full";
                    break;
                case GalleryState.Loaded:
                    BookmarkBtn.Label = "Bookmark this gallery";
                    break;
                case GalleryState.Loading:
                    BookmarkBtn.Label = "Loading images...";
                    break;
                case GalleryState.Empty:
                    BookmarkBtn.Label = "";
                    break;
            }
        }

        public static void WaitBookmarking() {
            while (_galleryState == GalleryState.Bookmarking) {
                Task.Delay(10).Wait();
            }
        }
    }
}
