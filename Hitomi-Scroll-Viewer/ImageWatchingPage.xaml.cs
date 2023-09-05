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
        private static Image[] _images;

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

        private GalleryState _galleryState = GalleryState.Empty;

        private static readonly ManualResetEventSlim _pageTurnSignal = new(true);
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
                },
                Timeout = TimeSpan.FromSeconds(5)
            };
        }

        private void SetScrollSpeedSlider() {
            switch (_viewMode) {
                case ViewMode.Default:
                    // save prev value because SetScrollSpeed is called when min max is set
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
                    // save prev value because SetScrollSpeed is called when min max is set
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
                PageNumDisplay.Text = $"Page {_currPage + 1} of {_mw.gallery.files.Length}";
            }
        }

        private void HandleGoBackBtnClick(object _0, RoutedEventArgs _1) {
            _mw.SwitchPage();
        }

        private void HandleAutoScrollBtnClick(object _0, RoutedEventArgs _1) {
            StartStopAutoScroll(!_isAutoScrolling);
        }

        // TODO
        // only reload the missing images by checking which files are missing and passing those image indexes
        // horizontal and vertical image scrolling in any direction
        /**
         * 1. user loads image
         * 2. disable:
         * - Load Images: button and enter input @
         * - all bookmark load button @
         * - change view mode button @
         * - bookmark button @
         * - image size slider @
         * - auto scroll: button and spacebar input @
         * - reload button @
         * - max concurrent button @
         * 3. disable left/right keys
         * 4. create Image[] and add to scrollview
         * 5. enable left/right keys
         * 6. change reload text and icon to cancel loading: Click to cancel gallery loading
         * when cancel button pressed:
         * disable cancel button and change text to "cancelling gallery loading..."
         * 7. after cancellation is done enable all back and change cancel button text to reload text
         * 
         */

        private async void HandleLoadingControlBtnClick(object _0, RoutedEventArgs _1) {
            // TODO calculate missing indexes
            bool isInAction;
            lock (_mw.actionLock) {
                isInAction = _mw.isInAction;
                if (isInAction) {
                    LoadingControlBtn.IsEnabled = false;
                    _mw.cts.Cancel();
                }
            }
            if (isInAction) {
                while (_mw.isInAction) {
                    await Task.Delay(10);
                }
                _mw.cts.Dispose();
                LoadingControlBtn.IsEnabled = true;
            } else {
                // reload gallery
                if (_mw.gallery != null) {
                    ReloadGallery();
                }
            }
            
        }
        
        private async void ReloadGallery() {

        }

        public void StartStopAutoScroll(bool start) {
            _isAutoScrolling = start;
            AutoScrollBtn.IsChecked = start;
            stopwatch.Reset();
            if (start) {
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
            PageNumDisplay.Text = $"Page {_currPage + 1} of {_mw.gallery.files.Length}";
            ImageContainer.Children.Clear();
            ImageContainer.Children.Add(_images[_currPage]);
        }

        private void InsertImages() {
            ImageContainer.Children.Clear();
            for (int i = 0; i < _images.Length; i++) {
                ImageContainer.Children.Add(_images[i]);
            }
        }

        private async void HandleViewModeBtnClick(object _0, RoutedEventArgs _1) {
            _mw.StartAction(true);
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
            _mw.StartAction(false);
        }

        private static async Task WaitImageLoad() {
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
            if (_currPage == 0) return 0;
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
            _currPage = (_currPage + num + _mw.gallery.files.Length) % _mw.gallery.files.Length;
        }

        public void HandleKeyDown(object _, KeyRoutedEventArgs e) {
            if (e.Key == Windows.System.VirtualKey.L) {
                LoopBtn.IsChecked = !LoopBtn.IsChecked;
            }
            if (_mw.gallery != null) {
                if (_viewMode == ViewMode.Default && _pageTurnSignal.IsSet) {
                    _pageTurnSignal.Reset();
                    if (e.Key == Windows.System.VirtualKey.Right) {
                        IncrementPage(1);
                        InsertSingleImage();
                    }
                    else if (e.Key == Windows.System.VirtualKey.Left) {
                        IncrementPage(-1);
                        InsertSingleImage();
                    }
                    _pageTurnSignal.Set();
                }
                if (e.Key == Windows.System.VirtualKey.Space && AutoScrollBtn.IsEnabled) {
                    StartStopAutoScroll(!_isAutoScrolling);
                }
            }
        }

        // for updating auto scrolling in sync with real time
        private static readonly Stopwatch stopwatch = new();

        private async void ScrollAutomatically() {
            while (_isAutoScrolling) {
                switch (_viewMode) {
                    case ViewMode.Default:
                        if (_currPage + 1 == _mw.gallery.files.Length && !_isLooping) {
                            DispatcherQueue.TryEnqueue(() => StartStopAutoScroll(false));
                            return;
                        }
                        await Task.Delay((int)(_pageTurnDelay * 1000));
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
                for (int i = 0; i < _images.Length; i++) {
                    _images[i].Width = _mw.gallery.files[i].width * _imageScale;
                    _images[i].Height = _mw.gallery.files[i].height * _imageScale;
                }
                if (_viewMode == ViewMode.Scroll) {
                    // wait for the actual image heights to be updated
                    double PIXEL_MARGIN = 1;
                    bool heightAllSet = false;
                    int startIdx = 0;
                    while (!heightAllSet) {
                        heightAllSet = true;
                        for (int i = startIdx; i < _images.Length; i++) {
                            // if actual height is not within the expected height range 
                            if (_images[i].ActualHeight < _images[i].Height - PIXEL_MARGIN
                                || _images[i].ActualHeight > _images[i].Height + PIXEL_MARGIN) {
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
            if (_galleryState != GalleryState.Empty) {
                ViewModeBtn.IsEnabled = enable;
                ImageScaleSlider.IsEnabled = enable;
                AutoScrollBtn.IsEnabled = enable;
            }
            MaxCncrRequestSelector.IsEnabled = enable;
        }

        private void StartLoading() {
            _mw.StartAction(true);
            LoadingProgressBar.Value = 0;
            LoadingProgressBar.Visibility = Visibility.Visible;
            // check if we have a gallery already loaded
            if (_mw.gallery != null) {
                // if the loaded gallery is not bookmarked delete it from local directory
                if (!_mw.IsBookmarked()) {
                    DeleteGallery(_mw.gallery);
                }
            }
        }

        private void FinishLoading(GalleryState state) {
            LoadingProgressBar.Visibility = Visibility.Collapsed;
            ChangeBookmarkBtnState(state);
            _mw.StartAction(false);
        }

        public void LoadGalleryFromLocalDir(Gallery gallery) {
            StartLoading();
            _mw.gallery = gallery;
            _images = new Image[gallery.files.Length];
            LoadingProgressBar.Maximum = gallery.files.Length;
            string dir = IMAGE_DIR + @"\" + gallery.id + @"\";
            for (int i = 0; i < _images.Length; i++) {
                _images[i] = new() {
                    Source = new BitmapImage(new(dir + i + IMAGE_EXT)),
                    Width = gallery.files[i].width * _imageScale,
                    Height = gallery.files[i].height * _imageScale
                };
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
            catch (TaskCanceledException) {
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
            catch (TaskCanceledException) {
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
            catch (TaskCanceledException) {
                return null;
            }
            return await response.Content.ReadAsByteArrayAsync(ct);
        }

        private async Task TryGetImageBytesFromWeb(string imgAddress, int idx, CancellationToken ct) {
            foreach (string subdomain in POSSIBLE_IMAGE_SUBDOMAINS) {
                if (ct.IsCancellationRequested) {
                    return;
                }
                byte[] imageBytes = await GetImageBytesFromWeb(subdomain + imgAddress, ct);
                if (imageBytes != null) {
                    await File.WriteAllBytesAsync(IMAGE_DIR + @"\" + _mw.gallery.id + @"\" + idx + IMAGE_EXT, imageBytes, ct);
                    break;
                }
            }
            DispatcherQueue.TryEnqueue(() => {
                LoadingProgressBar.Value++;
            });
        }

        public async Task LoadGalleryFromWeb(string id, int pageNum) {
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

            try {
                _mw.gallery = JsonSerializer.Deserialize<Gallery>(galleryInfo, serializerOptions);
            } catch (JsonException e) {
                _mw.AlertUser("Error while reading gallery json file", e.Message);
                FinishLoading(GalleryState.Empty);
                return;
            }

            LoadingProgressBar.Maximum = _mw.gallery.files.Length;

            string[] imgHashArr = new string[_mw.gallery.files.Length];
            for (int i = 0; i < _mw.gallery.files.Length; i++) {
                imgHashArr[i] = _mw.gallery.files[i].hash;
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

            Directory.CreateDirectory(IMAGE_DIR + @"\" + id);
            // example:
            // images length = 47, _currMaxCncrReq = 3
            // 47 / 3 = 15 r 2
            // 15+1 | 15+1 | 15
            int quotient = imgAddresses.Length / _currMaxCncrReq;
            int remainder = imgAddresses.Length % _currMaxCncrReq;
            Task[] tasks = new Task[_currMaxCncrReq];

            int startIdx = 0;
            for (int i = 0; i < _currMaxCncrReq; i++) {
                int thisI = i;
                int thisStartIdx = startIdx;
                Debug.WriteLine("creating task " + i);
                tasks[i] = Task.Run(async () => {
                    for (int j = 0; j < 1000; j++) {
                        await Task.Delay(50);
                        Debug.WriteLine("task " + thisI + ", " + j);
                        if (ct.IsCancellationRequested) {
                            Debug.WriteLine("yeah cancellation requested: " + thisI);
                            break;
                        }
                    }
                });
                //tasks[i] = Task.Run(async () => {

                    //for (int j = 0; j < quotient + (thisI < remainder ? 1 : 0); j++) {
                    //    if (ct.IsCancellationRequested) {
                    //        return;
                    //    }
                    //    int idx = thisStartIdx + j;
                    //    await TryGetImageBytesFromWeb(imgAddresses[idx], idx);
                    //}
                //});
                startIdx += quotient + (i < remainder ? 1 : 0);
            }

            Debug.WriteLine("load gallery 1111");

            Task all = Task.WhenAll(tasks);

            Debug.WriteLine("load gallery 2222");

            await Task.WhenAll(tasks);

            Debug.WriteLine("load gallery 3333");
            if (ct.IsCancellationRequested) {
                FinishLoading(GalleryState.Empty);
                return;
            }

            // TODO _pageTurnSignal

            //_images = new Image[gallery.files.Length];
            //string dir = IMAGE_DIR + @"\" + gallery.id + @"\";
            //string missingIndexesText = "";
            //bool atLeastOneMissing = false;
            //for (int i = 0; i < gallery.files.Length; i++) {
            //    if (ct.IsCancellationRequested) {
            //        FinishLoading(GalleryState.Empty);
            //        return;
            //    }
            //    string path = dir + i + IMAGE_EXT;
            //    BitmapImage source = null;
            //    if (File.Exists(path)) {
            //        source = new BitmapImage(new(path));
            //    } else {
            //        atLeastOneMissing = true;
            //        missingIndexesText += i + 1 + ", ";
            //    }
            //    _images[i] = new() {
            //        Source = source,
            //        Width = gallery.files[i].width * _imageScale,
            //        Height = gallery.files[i].height * _imageScale
            //    };
            //}
            //Debug.WriteLine("load gallery 6666");
            //_currPage = pageNum;
            //switch (_viewMode) {
            //    case ViewMode.Default:
            //        InsertSingleImage();
            //        break;
            //    case ViewMode.Scroll:
            //        InsertImages();
            //        await WaitImageLoad();
            //        DispatcherQueue.TryEnqueue(() => MainScrollViewer.ScrollToVerticalOffset(GetScrollOffsetFromPage()));
            //        break;
            //}
            //Debug.WriteLine("load gallery 7777");
            //if (atLeastOneMissing) {
            //    _mw.AlertUser("The image at the following pages have failed to load. Try reducing max concurrent request if the problem persists.", missingIndexesText[..^2]);
            //}

            //if (IsBookmarkFull()) {
            //    FinishLoading(GalleryState.BookmarkFull);
            //}
            //else if (IsBookmarked()) {
            //    FinishLoading(GalleryState.Bookmarked);
            //}
            //else {
            //    FinishLoading(GalleryState.Loaded);
            //}
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

        public void WaitBookmarking() {
            bookmarkSignal.Wait();
            bookmarkSignal.Dispose();
        }
    }
}
