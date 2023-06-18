using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
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

        private static int _currPage = 0;

        private enum ViewMode {
            Default,
            Scroll
        }
        private static ViewMode _viewMode = ViewMode.Default;

        private static readonly HttpClient _httpClient = new();
        private static readonly string GALLERY_INFO_DOMAIN = "https://ltn.hitomi.la/galleries/";
        private static readonly string SERVER_TIME_ADDRESS = "https://ltn.hitomi.la/gg.js";
        private static readonly string REFERER = "https://hitomi.la/";
        private static readonly string[] POSSIBLE_IMAGE_SUBDOMAINS = { "https://aa.", "https://ba." };
        private static readonly JsonSerializerOptions serializerOptions = new() { IncludeFields = true };

        private static CancellationTokenSource _cts = new();
        private static CancellationToken _ct = _cts.Token;

        public enum GalleryState {
            Bookmarked,
            Loaded,
            Loading,
            BookmarkFull,
            Empty
        }
        private static bool _isLoading = false;

        private static int _loadRequestCounter = 0;

        public ImageWatchingPage(MainWindow mainWindow) {
            InitializeComponent();
            _mw = mainWindow;

            // handle mouse movement on commandbar
            void handlePointerEnter(object commandBar, PointerRoutedEventArgs args) {
                ((CommandBar)commandBar).IsOpen = true;
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
                }
            }
            TopCommandBar.PointerMoved += handlePointerMove;
        }

        public void Init(SearchPage sp) {
            BookmarkBtn.Click += sp.AddBookmark;
        }

        private void HandleGoBackBtnClick(object _, RoutedEventArgs e) {
            _mw.SwitchPage();
        }

        private void HandleLoopToggleBtnClick(object _, RoutedEventArgs e) {
            _isLooping = !_isLooping;
        }

        public static void StopAutoScrolling() {
            _isAutoScrolling = false;
            stopwatch.Reset();
        }

        private async void SetCurrGalleryPage() {
            // accessing UI thread {
            ImageContainer.Children.Clear();
            // }
            string path = IMAGE_DIR + @"\" + gallery.id;
            Image image = new() {
                Source = await GetBitmapImage(await File.ReadAllBytesAsync(path + @"\" + _currPage)),
                Width = gallery.files[_currPage].width * _imageScale,
                Height = gallery.files[_currPage].height * _imageScale,
            };
            // accessing UI thread {
            ImageContainer.Children.Add(image);
            // }
        }

        private async void HandleViewModeBtnClick(object _, RoutedEventArgs e) {
            if (!await RequestLoadPermit()) {
                return;
            }
            StartLoading();
            switch (_viewMode) {
                case ViewMode.Default:
                    _viewMode = ViewMode.Scroll;
                    break;
                case ViewMode.Scroll:
                    _viewMode = ViewMode.Default;
                    break;
            }
            if (gallery == null) {
                FinishLoading(GalleryState.Empty);
                return;
            }
            try {
                switch (_viewMode) {
                    case ViewMode.Default:
                        _ct.ThrowIfCancellationRequested();
                        GetCurrScrollPage();
                        _ct.ThrowIfCancellationRequested();
                        SetCurrGalleryPage();
                        break;
                    case ViewMode.Scroll:
                        _ct.ThrowIfCancellationRequested();
                        // accessing UI thread {
                        ImageContainer.Children.Clear();
                        // }
                        Image[] images = new Image[gallery.files.Count];
                        string path = IMAGE_DIR + @"\" + gallery.id;

                        for (int i = 0; i < images.Length; i++) {
                            _ct.ThrowIfCancellationRequested();
                            images[i] = new() {
                                Source = await GetBitmapImage(await File.ReadAllBytesAsync(path + @"\" + i.ToString())),
                                Width = gallery.files[i].width * _imageScale,
                                Height = gallery.files[i].height * _imageScale,
                            };
                        }
                        // accessing UI thread {
                        for (int i = 0; i < images.Length; i++) {
                            _ct.ThrowIfCancellationRequested();
                            ImageContainer.Children.Add(images[i]);
                        }
                        // }
                        MainScrollViewer.ScrollToVerticalOffset(GetScrollOffset());
                        break;
                }
                if (IsBookmarked()) {
                    FinishLoading(GalleryState.Bookmarked);
                }
                else if (IsBookmarkFull()) {
                    FinishLoading(GalleryState.BookmarkFull);
                }
                else {
                    FinishLoading(GalleryState.Loaded);
                }
            }
            catch (OperationCanceledException) {
                FinishLoading(GalleryState.Loading);
            }
        }

        private void GetCurrScrollPage() {
            double imageHeightSum = 0;
            for (int i = 0; i < gallery.files.Count; i++) {
                imageHeightSum += ((Image)ImageContainer.Children[i]).Height;
                if (imageHeightSum > MainScrollViewer.VerticalOffset) {
                    _currPage = i;
                    break;
                }
            }
        }

        private double GetScrollOffset() {
            double offset = 0;
            for (int i = 0; i < gallery.files.Count; i++) {
                if (i < _currPage) {
                    offset += ((Image)ImageContainer.Children[i]).Height;
                }
            }
            return offset;
        }

        private void SetScrollSpeed(object slider, RangeBaseValueChangedEventArgs e) {
            _scrollSpeed = (slider as Slider).Value;
        }

        private static void IncrementPage(int num) {
            _currPage = (_currPage + num + gallery.files.Count) % gallery.files.Count;
        }

        public void HandleKeyDown(object _, KeyRoutedEventArgs e) {
            if (gallery != null && !_isLoading) {
                switch (_viewMode) {
                    case ViewMode.Default:
                        if (e.Key == Windows.System.VirtualKey.Left || e.Key == Windows.System.VirtualKey.LeftButton) {
                            IncrementPage(1);
                            SetCurrGalleryPage();
                        } else if (e.Key == Windows.System.VirtualKey.Right || e.Key == Windows.System.VirtualKey.RightButton) {
                            IncrementPage(-1);
                            SetCurrGalleryPage();
                        }
                        if (e.Key == Windows.System.VirtualKey.Space) {
                            if (_isAutoScrolling = !_isAutoScrolling) {
                                Task.Run(ScrollAutomatically);
                            }
                        }
                        break;
                    case ViewMode.Scroll:
                        if (e.Key == Windows.System.VirtualKey.Space) {
                            stopwatch.Reset();
                            if (_isAutoScrolling = !_isAutoScrolling) {
                                stopwatch.Start();
                                Task.Run(ScrollAutomatically);
                            }
                        }
                        break;
                }
            }
        }

        // for updating auto scrolling in sync with real time
        private static readonly Stopwatch stopwatch = new();

        private async void ScrollAutomatically() {
            while (_isAutoScrolling) {
                switch (_viewMode) {
                    case ViewMode.Default:
                        // delay = -1.9*(slider value) + 11 in seconds
                        double delay = (-1.9 * _scrollSpeed + 11) * 1000;
                        await Task.Delay((int)delay);
                        IncrementPage(1);
                        // accessing UI thread {
                        DispatcherQueue.TryEnqueue(SetCurrGalleryPage);
                        // }
                        break;
                    case ViewMode.Scroll:
                        // accessing UI thread {
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
                            }
                        });
                        // }
                        break;
                }
            }
        }

        private double _imageScale = 1;

        /**
         * <summary>
         * Change image size and re-position vertical offset to the page that the user was already at.
         * </summary>
         */
        private void ChangeImageSize(object slider, RangeBaseValueChangedEventArgs e) {
            _imageScale = ((Slider)slider).Value;
            if (ImageContainer != null && gallery != null) {
                switch (_viewMode) {
                    case ViewMode.Default:
                        Image image = (Image)ImageContainer.Children[0];
                        image.Width = gallery.files[_currPage].width * _imageScale;
                        image.Height = gallery.files[_currPage].height * _imageScale;
                        break;
                    case ViewMode.Scroll:
                        GetCurrScrollPage();
                        for (int i = 0; i < ImageContainer.Children.Count; i++) {
                            (ImageContainer.Children[i] as Image).Width = gallery.files[i].width * _imageScale;
                            (ImageContainer.Children[i] as Image).Height = gallery.files[i].height * _imageScale;
                        }
                        // set vertical offset according to the new image size scale
                        MainScrollViewer.ScrollToVerticalOffset(GetScrollOffset());
                        break;
                }

            }
        }

        private void StartLoading() {
            _isLoading = true;
            StopAutoScrolling();
            ViewModeBtn.IsEnabled = false;
            ImageScaleSlider.IsEnabled = false;
            ChangeBookmarkBtnState(GalleryState.Loading);
        }

        private void FinishLoading(GalleryState state) {
            _isLoading = false;
            ViewModeBtn.IsEnabled = true;
            ImageScaleSlider.IsEnabled = true;
            ChangeBookmarkBtnState(state);
        }

        // TODO Debug overall loading algorithm
        /**
         * <summary>Returns <c>true</c> if the load request is permitted, otherwise <c>false</c>.</summary>
         */
        private static async Task<bool> RequestLoadPermit() {
            if (_isLoading) {
                int rank = Interlocked.Increment(ref _loadRequestCounter);
                if (rank == 1) {
                    // first load request so send cancel request
                    _cts.Cancel();
                }
                while (_isLoading) {
                    await Task.Delay(10);
                    // this request is not the latest request anymore
                    if (_loadRequestCounter != rank) {
                        return false;
                    }
                }
                // loading finished
                // this request is the latest request
                _isLoading = true;
                _loadRequestCounter = 0;
                _cts.Dispose();
                _cts = new();
                _ct = _cts.Token;
                return true;
            }
            _isLoading = true;
            return true;
        }

        private async Task<bool> PrepareImageLoad() {
            if (!await RequestLoadPermit()) {
                return false;
            }
            StartLoading();
            // accessing UI thread {
            ImageContainer.Children.Clear();
            _mw.SwitchPage();
            // }
            // check if we have a gallery already loaded
            if (gallery != null) {
                // if the loaded gallery is not bookmarked delete it from local directory
                if (!IsBookmarked()) {
                    DeleteGallery(gallery.id);
                }
            }
            return true;
        }

        public async Task LoadGalleryFromLocalDir(int bmIdx) {
            if (!await PrepareImageLoad()) {
                return;
            }
            gallery = bmGalleries[bmIdx];
            string path = IMAGE_DIR + @"\" + bmGalleries[bmIdx].id;
            try {
                switch (_viewMode) {
                    case ViewMode.Default:
                        _ct.ThrowIfCancellationRequested();
                        _currPage = 0;
                        Image image = new() {
                            Source = await GetBitmapImage(await File.ReadAllBytesAsync(path + @"\" + _currPage)),
                            Width = bmGalleries[bmIdx].files[_currPage].width * _imageScale,
                            Height = bmGalleries[bmIdx].files[_currPage].height * _imageScale,
                        };
                        // accessing UI thread {
                        ImageContainer.Children.Add(image);
                        // }
                        break;
                    case ViewMode.Scroll:
                        _ct.ThrowIfCancellationRequested();
                        Image[] images = new Image[bmGalleries[bmIdx].files.Count];
                        for (int i = 0; i < images.Length; i++) {
                            _ct.ThrowIfCancellationRequested();
                            images[i] = new() {
                                Source = await GetBitmapImage(await File.ReadAllBytesAsync(path + @"\" + i.ToString())),
                                Width = bmGalleries[bmIdx].files[i].width * _imageScale,
                                Height = bmGalleries[bmIdx].files[i].height * _imageScale,
                            };
                        }
                        // accessing UI thread {
                        for (int i = 0; i < images.Length; i++) {
                            _ct.ThrowIfCancellationRequested();
                            ImageContainer.Children.Add(images[i]);
                        }
                        // }
                        break;
                }
                FinishLoading(GalleryState.Bookmarked);
            }
            catch (OperationCanceledException) {
                FinishLoading(GalleryState.Loading);
            }
        }

        private static async Task<string> GetGalleryInfo(string id) {
            string address = GALLERY_INFO_DOMAIN + id + ".js";
            HttpRequestMessage galleryInfoRequest = new() {
                Method = HttpMethod.Get,
                RequestUri = new Uri(address)
            };
            HttpResponseMessage response = await _httpClient.SendAsync(galleryInfoRequest);
            try {
                response.EnsureSuccessStatusCode();
            } catch (HttpRequestException ex) {
                _mw.AlertUser("An error has occurred while getting gallery info. Please try again.", ex.Message);
                return null;
            }
            string responseString = await response.Content.ReadAsStringAsync();
            for (int i = 0; i < responseString.Length; i++) {
                if (responseString[i] == '{') {
                    responseString = responseString[i..];
                    break;
                }
            }
            return responseString;
        }

        private static async Task<string> GetServerTime() {
            HttpRequestMessage serverTimeRequest = new() {
                Method = HttpMethod.Get,
                RequestUri = new Uri(SERVER_TIME_ADDRESS)
            };
            HttpResponseMessage response = await _httpClient.SendAsync(serverTimeRequest);
            try {
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException ex) {
                _mw.AlertUser("An error has occurred while getting server time. Please try again.", ex.Message);
                return null;
            }
            string responseString = await response.Content.ReadAsStringAsync();

            return Regex.Match(responseString, @"\'(.+?)/\'").Value[1..^2];
        }

        // TODO read about http request sockets, multithreading, concurrent requests, etc.
        // and implement them accordingly
        // search: c# concurrent http requests
        // to check if images are requested asynchrounously print out i for loop

        public async Task LoadImagesFromWeb(string id) {
            if (!await PrepareImageLoad()) {
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

                _ct.ThrowIfCancellationRequested();

                string[] imgHashArr = new string[gallery.files.Count];
                for (int i = 0; i < gallery.files.Count; i++) {
                    imgHashArr[i] = gallery.files[i].hash;
                }

                string serverTime = await GetServerTime();
                string[] imgAddresses;
                if (serverTime != null) {
                    FinishLoading(GalleryState.Empty);
                    return;
                }
                imgAddresses = GetImageAddresses(imgHashArr, serverTime);

                _ct.ThrowIfCancellationRequested();

                byte[][] imageBytes = new byte[imgAddresses.Length][];

                for (int i = 0; i < imgAddresses.Length; i++) {
                    _ct.ThrowIfCancellationRequested();
                    foreach (string subdomain in POSSIBLE_IMAGE_SUBDOMAINS) {
                        imageBytes[i] = await GetImageBytesFromWeb(subdomain + imgAddresses[i]);
                    }
                }

                // save gallery to local directory
                await SaveGallery(gallery.id, imageBytes);

                switch (_viewMode) {
                    case ViewMode.Default:
                        _ct.ThrowIfCancellationRequested();
                        _currPage = 0;
                        Image image = new() {
                            Source = await GetBitmapImage(imageBytes[_currPage]),
                            Width = gallery.files[_currPage].width * _imageScale,
                            Height = gallery.files[_currPage].height * _imageScale
                        };
                        // accessing UI thread {
                        ImageContainer.Children.Add(image);
                        // }
                        break;
                    case ViewMode.Scroll:
                        _ct.ThrowIfCancellationRequested();
                        Image[] images = new Image[imgAddresses.Length];
                        for (int i = 0; i < imgAddresses.Length; i++) {
                            _ct.ThrowIfCancellationRequested();
                            images[i] = new() {
                                Source = await GetBitmapImage(imageBytes[i]),
                                Width = gallery.files[i].width * _imageScale,
                                Height = gallery.files[i].height * _imageScale
                            };
                        }
                        // accessing UI thread {
                        for (int i = 0; i < images.Length; i++) {
                            _ct.ThrowIfCancellationRequested();
                            ImageContainer.Children.Add(images[i]);
                        }
                        // }
                        break;
                }
                if (IsBookmarkFull()) {
                    FinishLoading(GalleryState.BookmarkFull);
                }
                else {
                    FinishLoading(GalleryState.Loaded);
                }
            }
            catch (OperationCanceledException) {
                FinishLoading(GalleryState.Loading);
                return;
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

        public static async Task<byte[]> GetImageBytesFromWeb(string address) {
            HttpRequestMessage request = new() {
                Method = HttpMethod.Get,
                RequestUri = new Uri(address),
                Headers = {
                    {"referer", REFERER }
                },
            };
            HttpResponseMessage response = await _httpClient.SendAsync(request);
            try {
                response.EnsureSuccessStatusCode();
            } catch (HttpRequestException) {
                return null;
            }
            return await response.Content.ReadAsByteArrayAsync();
        }

        public void ChangeBookmarkBtnState(GalleryState state) {
            switch (state) {
                case GalleryState.Bookmarked:
                    BookmarkBtn.Label = "Bookmarked";
                    BookmarkBtn.IsEnabled = false;
                    break;
                case GalleryState.Loading:
                    BookmarkBtn.Label = "Loading Images...";
                    BookmarkBtn.IsEnabled = false;
                    break;
                case GalleryState.Loaded:
                    BookmarkBtn.Label = "Bookmark this Gallery";
                    BookmarkBtn.IsEnabled = true;
                    break;
                case GalleryState.BookmarkFull:
                    BookmarkBtn.Label = "Bookmark is full";
                    BookmarkBtn.IsEnabled = false;
                    break;
                case GalleryState.Empty:
                    BookmarkBtn.Label = "";
                    BookmarkBtn.IsEnabled = false;
                    break;
            }
        }
    }
}
