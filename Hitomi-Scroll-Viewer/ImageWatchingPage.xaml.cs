using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using static Hitomi_Scroll_Viewer.MainWindow;

namespace Hitomi_Scroll_Viewer {
    public sealed partial class ImageWatchingPage : Page {
        private static MainWindow _mw;

        private static bool _isAutoScrolling = false;
        private static bool _isLooping = true;
        private static double _scrollSpeed;
        private static double _pageTurnDelay;
        private static double _imageScale = 1;

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
        public static GalleryState galleryState = GalleryState.Empty; 
        private static bool _isInAction = false;

        private static int _loadRequestCounter = 0;

        public ImageWatchingPage(MainWindow mainWindow) {
            InitializeComponent();

            StartAction();

            _mw = mainWindow;

            // Set ImageContainer top margin based on top commandbar height
            void handleSizeChange(object cb, SizeChangedEventArgs e) {
                ImageContainer.Margin = new Thickness(0, ((CommandBar)cb).ActualHeight, 0, 0);
            }

            TopCommandBar.SizeChanged += handleSizeChange;

            // handle mouse movement on commandbar
            void handlePointerEnter(object cb, PointerRoutedEventArgs args) {
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

            SocketsHttpHandler shh = new() {
                MaxConnectionsPerServer = 30,
            };
            _httpClient = new(shh);
        }

        public void Init(SearchPage sp) {
            BookmarkBtn.Click += sp.AddBookmark;
        }

        private void HandleScrollViewChange(object _, ScrollViewerViewChangingEventArgs e) {
            if (_viewMode == ViewMode.Scroll) {
                GetPageFromScrollOffset();
                PageNumDisplay.Text = $"Page {_currPage + 1} of {gallery.files.Count}";
            }
        }

        private void HandleGoBackBtnClick(object _, RoutedEventArgs e) {
            _mw.SwitchPage();
        }

        private void HandleAutoScrollBtnClick(object _, RoutedEventArgs e) {
            SetAutoScroll(!_isAutoScrolling);
        }

        public void SetAutoScroll(bool newValue) {
            _isAutoScrolling = newValue;
            stopwatch.Reset();
            AutoScrollBtn.IsChecked = newValue;
            if (newValue) {
                AutoScrollBtn.Icon = new SymbolIcon(Symbol.Pause);
                AutoScrollBtn.Label = "Stop";
                Task.Run(ScrollAutomatically);
            } else {
                AutoScrollBtn.Icon = new SymbolIcon(Symbol.Play);
                AutoScrollBtn.Label = "Start Auto Page Turning / Scrolling";
            }
        }

        private void HandleLoopBtnClick(object _, RoutedEventArgs e) {
            _isLooping = !_isLooping;
        }

        private void SetLoop(bool newValue) {
            _isLooping = newValue;
            LoopBtn.IsChecked = newValue;
        }

        // https://hitomi.la/doujinshi/kameki-%E6%97%A5%E6%9C%AC%E8%AA%9E-2561144.html#1
        // https://hitomi.la/doujinshi/radiata-%E6%97%A5%E6%9C%AC%E8%AA%9E-2472850.html#1
        // TODO make image loading from web not asynchronous and see if it solves 503 error
        // because it is likely due to sending too many requests at a time
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

        private async void HandleViewModeBtnClick(object _, RoutedEventArgs e) {
            if (!await RequestActionPermit()) {
                return;
            }
            StartAction();
            try {
                switch (_viewMode) {
                    case ViewMode.Default:
                        _viewMode = ViewMode.Scroll;
                        InsertImages();
                        bool allLoaded = false;
                        // wait for the actual image heights to be updated
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
            }
            catch (OperationCanceledException) {}
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
                imageHeightSum += _images[i].Height;
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
                offset += _images[i].Height;
            }
            return offset;
        }

        private void SetScrollSpeed(object slider, RangeBaseValueChangedEventArgs e) {
            _scrollSpeed = ((Slider)slider).Value;
            // hardcoded value linear equation
            _pageTurnDelay = (-1.9 * _scrollSpeed + 11) * 1000;
        }

        private static void IncrementPage(int num) {
            _currPage = (_currPage + num + gallery.files.Count) % gallery.files.Count;
        }

        public void HandleKeyDown(object _, KeyRoutedEventArgs e) {
            if (e.Key == Windows.System.VirtualKey.L) {
                SetLoop(!_isLooping);
            }
            if (gallery != null && !_isInAction) {
                _isInAction = true;
                if (_viewMode == ViewMode.Default) {
                    if (e.Key is Windows.System.VirtualKey.Right or Windows.System.VirtualKey.RightButton) {
                        IncrementPage(1);
                        InsertSingleImage();
                    } else if (e.Key is Windows.System.VirtualKey.Left or Windows.System.VirtualKey.LeftButton) {
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
                        await Task.Delay((int)_pageTurnDelay);
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
                        DispatcherQueue.TryEnqueue(() => {
                            if (MainScrollViewer.VerticalOffset != MainScrollViewer.ScrollableHeight) {
                                stopwatch.Stop();
                                MainScrollViewer.ScrollToVerticalOffset(MainScrollViewer.VerticalOffset + _scrollSpeed * stopwatch.ElapsedMilliseconds);
                                stopwatch.Restart();
                            }
                            else {
                                if (_isLooping) {
                                    MainScrollViewer.ScrollToVerticalOffset(0);
                                } else {
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
        private async void ChangeImageSize(object slider, RangeBaseValueChangedEventArgs e) {
            _imageScale = ((Slider)slider).Value;
            if (ImageContainer != null && gallery != null) {
                switch (_viewMode) {
                    case ViewMode.Default:
                        _images[_currPage].Width = gallery.files[_currPage].width * _imageScale;
                        _images[_currPage].Height = gallery.files[_currPage].height * _imageScale;
                        break;
                    case ViewMode.Scroll:
                        double scrollableHeight = MainScrollViewer.ScrollableHeight;
                        GetPageFromScrollOffset();
                        for (int i = 0; i < _images.Length; i++) {
                            _images[i].Width = gallery.files[i].width * _imageScale;
                            _images[i].Height = gallery.files[i].height * _imageScale;
                        }
                        // wait for the actual image heights to be updated
                        while (scrollableHeight == MainScrollViewer.ScrollableHeight) {
                            await Task.Delay(10);
                        }
                        // set vertical offset according to the new image scale
                        MainScrollViewer.ScrollToVerticalOffset(GetScrollOffsetFromPage());
                        break;
                }
            }
        }

        /**
         * <summary>
         * <see cref="RequestActionPermit"/> or <see cref="StartLoading"/> must be called before calling this method
         * </summary>
         */
        private void StartAction() {
            SetAutoScroll(false);
            ViewModeBtn.IsEnabled = false;
            ImageScaleSlider.IsEnabled = false;
            AutoScrollBtn.IsEnabled = false;
        }

        private void StopAction() {
            ViewModeBtn.IsEnabled = true;
            ImageScaleSlider.IsEnabled = true;
            AutoScrollBtn.IsEnabled = true;
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
            _mw.SwitchPage();
            if (!await RequestActionPermit()) {
                return false;
            }

            StartAction();

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
            catch (OperationCanceledException) {}
            finally {
                if (_ct.IsCancellationRequested) {
                    FinishLoading(GalleryState.Loading);
                } else {
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

        // TODO read about http request sockets, multithreading, concurrent requests, etc.
        // and implement them accordingly
        // search: c# concurrent http requests
        // to check if images are requested asynchrounously print out i for loop

        // TODO reload button

        private async Task<byte[]> TryGetImageBytesFromWeb(string imgAddress) {
            byte[] imageBytes;
            _ct.ThrowIfCancellationRequested();
            foreach (string subdomain in POSSIBLE_IMAGE_SUBDOMAINS) {
                imageBytes = await GetImageBytesFromWeb(subdomain + imgAddress);
                if (imageBytes != null) {
                    LoadingProgressBar.Value++;
                    return imageBytes;
                }
            }
            return null;
        }

        public async Task LoadImagesFromWeb(string id) {
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

                Task<byte[]>[] tasks = new Task<byte[]>[imgAddresses.Length];

                for (int i = 0; i < imgAddresses.Length; i++) {
                    _ct.ThrowIfCancellationRequested();
                    tasks[i] = TryGetImageBytesFromWeb(imgAddresses[i]);
                }

                await Task.WhenAll(tasks);

                byte[][] imageBytes = new byte[tasks.Length][];

                for (int i = 0; i < imageBytes.Length; i++) {
                    if (tasks[i] != null) {
                        imageBytes[i] = tasks[i].Result;
                    }
                }

                // save gallery to local directory
                await SaveGallery(gallery.id, imageBytes);

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
            catch (OperationCanceledException) {}
            finally {
                if (_ct.IsCancellationRequested) {
                    FinishLoading(GalleryState.Loading);
                } else if (IsBookmarkFull()) {
                    FinishLoading(GalleryState.BookmarkFull);
                } else {
                    FinishLoading(GalleryState.Loaded);
                }
                // hide LoadingProgressBar
                LoadingProgressBar.Visibility = Visibility.Collapsed;
            }
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
                Headers = {
                    {"referer", REFERER }
                },
            };
            HttpResponseMessage response;
            try {
                response = await _httpClient.SendAsync(request, _ct);
                response.EnsureSuccessStatusCode();
            } catch (HttpRequestException e) {
                if (e.StatusCode != System.Net.HttpStatusCode.NotFound) {
                    Debug.WriteLine(e.Message);
                    Debug.WriteLine("Status Code: " + e.StatusCode);
                }
                return null;
            } catch (TaskCanceledException) {
                return null;
            }
            return await response.Content.ReadAsByteArrayAsync();
        }

        public void ChangeBookmarkBtnState(GalleryState state) {
            galleryState = state;
            if (state == GalleryState.Loaded) {
                BookmarkBtn.IsEnabled = true;
            } else {
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
    }
}
