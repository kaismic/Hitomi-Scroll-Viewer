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

        public static bool isAutoScrolling = false;
        private static bool _isLooping;
        private static double _scrollSpeed;

        private static int _currGalleryPageNum = 0;

        public enum ViewMode {
            Default,
            Scroll
        }
        private static ViewMode _viewMode = ViewMode.Default;

        private static double _commandBarShowRange = 0.08;

        private static readonly HttpClient _myHttpClient = new();
        private static readonly string GALLERY_INFO_DOMAIN = "https://ltn.hitomi.la/galleries/";
        private static readonly string SERVER_TIME_ADDRESS = "https://ltn.hitomi.la/gg.js";
        private static readonly string REFERER = "https://hitomi.la/";
        private static readonly string[] POSSIBLE_IMAGE_SUBDOMAINS = { "https://aa.", "https://ba." };
        private static readonly JsonSerializerOptions serializerOptions = new() { IncludeFields = true };

        private static CancellationTokenSource _cts = new();
        private static CancellationToken _ct;

        public enum LoadingState {
            Bookmarked,
            Loaded,
            Loading,
            BookmarkFull
        }
        private static bool _isLoading = false;

        public ImageWatchingPage(MainWindow mainWindow) {
            InitializeComponent();
            _mw = mainWindow;
            _isLooping = true;
            LoopBtn.IsChecked = true;

            // set _commandBarShowRange
            void loadHandler(object _, RoutedEventArgs e) {
                _commandBarShowRange *= ActualHeight;
                Loaded -= loadHandler;
            }
            Loaded += loadHandler;

            _ct = _cts.Token;
        }

        public void Init() {
            MainGrid.PointerMoved += HandleMouseMovement;
            BookmarkBtn.Click += _mw.sp.AddBookmark;
        }

        private void HandleGoBackBtnClick(object _, RoutedEventArgs e) {
            _mw.SwitchPage();
        }

        private void HandleLoopToggleBtnClick(object _, RoutedEventArgs e) {
            _isLooping = !_isLooping;
        }

        public static void StopAutoScrolling() {
            isAutoScrolling = false;
            stopwatch.Reset();
        }

        private async void SetCurrGalleryPage() {
            // accessing UI thread
            ImageContainer.Children.Clear();
            // accessing UI thread
            string path = IMAGE_DIR + @"\" + gallery.id;
            Image image = new() {
                Source = await GetBitmapImage(await File.ReadAllBytesAsync(path + @"\" + _currGalleryPageNum)),
                Width = gallery.files[_currGalleryPageNum].width * ImageScaleSlider.Value,
                Height = gallery.files[_currGalleryPageNum].height * ImageScaleSlider.Value,
            };
            // accessing UI thread
            ImageContainer.Children.Add(image);
            // accessing UI thread
        }

        private async void HandleViewModeChangeBtnClick(object _, RoutedEventArgs e) {
            string path = IMAGE_DIR + @"\" + gallery.id;
            switch (_viewMode) {
                case ViewMode.Default:
                    // accessing UI thread
                    ImageContainer.Children.Clear();
                    // accessing UI thread
                    _viewMode = ViewMode.Scroll;
                    Image[] images = new Image[gallery.files.Count];
                    for (int i = 0; i < images.Length; i++) {
                        images[i] = new() {
                            Source = await GetBitmapImage(await File.ReadAllBytesAsync(path + @"\" + i.ToString())),
                            Width = gallery.files[i].width * ImageScaleSlider.Value,
                            Height = gallery.files[i].height * ImageScaleSlider.Value,
                        };
                    }
                    // accessing UI thread
                    for (int i = 0; i < images.Length; i++) {
                        ImageContainer.Children.Add(images[i]);
                    }
                    MainScrollViewer.ScrollToVerticalOffset(GetScrollOffset());
                    // accessing UI thread
                    break;
                case ViewMode.Scroll:
                    _viewMode = ViewMode.Default;
                    StopAutoScrolling();
                    GetCurrScrollPage();
                    SetCurrGalleryPage();
                    break;
            }
        }

        private void GetCurrScrollPage() {
            double imageHeightSum = 0;
            for (int i = 0; i < ImageContainer.Children.Count; i++) {
                imageHeightSum += ((Image)ImageContainer.Children[i]).Height;
                if (imageHeightSum > MainScrollViewer.VerticalOffset) {
                    _currGalleryPageNum = i;
                    break;
                }
            }
        }

        private double GetScrollOffset() {
            double offset = 0;
            for (int i = 0; i < ImageContainer.Children.Count; i++) {
                if (i < _currGalleryPageNum) {
                    offset += ((Image)ImageContainer.Children[i]).Height;
                }
            }
            return offset;
        }

        private void HandleMouseMovement(object _, PointerRoutedEventArgs args) {
            Point pos = args.GetCurrentPoint(MainGrid).Position;
            if (pos.Y < _commandBarShowRange && pos.X < ActualWidth - 20) {
                if (!TopCommandBar.IsOpen) {
                    TopCommandBar.IsOpen = true;
                }
            }
            else {
                if (TopCommandBar.IsOpen) {
                    TopCommandBar.IsOpen = false;
                }
            }
        }

        private void SetScrollSpeed(object sender, RangeBaseValueChangedEventArgs e) {
            _scrollSpeed = (sender as Slider).Value;
        }

        public void HandleKeyDown(object _, KeyRoutedEventArgs e) {
            switch (_viewMode) {
                case ViewMode.Default:
                    if (e.Key == Windows.System.VirtualKey.Left || e.Key == Windows.System.VirtualKey.LeftButton) {
                        _currGalleryPageNum = (_currGalleryPageNum - 1 + gallery.files.Count) % gallery.files.Count;
                        SetCurrGalleryPage();
                    } else if (e.Key == Windows.System.VirtualKey.Right || e.Key == Windows.System.VirtualKey.RightButton) {
                        _currGalleryPageNum = (_currGalleryPageNum + 1) % gallery.files.Count;
                        SetCurrGalleryPage();
                    }
                    break;
                case ViewMode.Scroll:
                    if (e.Key == Windows.System.VirtualKey.Space) {
                        stopwatch.Reset();
                        if (isAutoScrolling = !isAutoScrolling) {
                            stopwatch.Start();
                            Task.Run(ScrollAutomatically);
                        }
                    }
                    break;
            }
        }

        // for updating scrolling in sync with real time
        private static readonly Stopwatch stopwatch = new();

        private void ScrollAutomatically() {
            while (isAutoScrolling) {
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
            }
        }

        /**
         * <summary>
         * Change image size and re-position vertical offset to the page that the user was already at.
         * </summary>
         */
        private void ChangeImageSize(object sender, RangeBaseValueChangedEventArgs e) {
            if (ImageContainer != null) {
                switch (_viewMode) {
                    case ViewMode.Default:
                        Image image = (Image)ImageContainer.Children[0];
                        image.Width = gallery.files[_currGalleryPageNum].width * ImageScaleSlider.Value;
                        image.Height = gallery.files[_currGalleryPageNum].height * ImageScaleSlider.Value;
                        break;
                    case ViewMode.Scroll:
                        GetCurrScrollPage();
                        for (int i = 0; i < ImageContainer.Children.Count; i++) {
                            (ImageContainer.Children[i] as Image).Width = gallery.files[i].width * ImageScaleSlider.Value;
                            (ImageContainer.Children[i] as Image).Height = gallery.files[i].height * ImageScaleSlider.Value;
                        }
                        // set vertical offset according to the new image size scale
                        MainScrollViewer.ScrollToVerticalOffset(GetScrollOffset());
                        break;
                }

            }
        }

        private static async Task CheckLoading() {
            if (_isLoading) {
                _cts.Cancel();
                while (_isLoading) {
                    await Task.Delay(10);
                }
                _cts.Dispose();
                _cts = new();
                _ct = _cts.Token;
            }
        }

        private async Task PrepareImageLoad() {
            await CheckLoading();

            ChangeBookmarkBtnState(LoadingState.Loading);
            _isLoading = true;

            // accessing UI thread
            ImageContainer.Children.Clear();
            _mw.SwitchPage();
            // accessing UI thread

            // check if we have a gallery already loaded
            if (gallery != null) {
                // if the loaded gallery is not bookmarked delete it from local directory
                if (!IsBookmarked()) {
                    DeleteGallery(gallery.id);
                }
            }
        }

        public async Task LoadGalleryFromLocalDir(int bmIdx) {
            await PrepareImageLoad();

            gallery = bmGalleries[bmIdx];

            string path = IMAGE_DIR + @"\" + bmGalleries[bmIdx].id;

            switch (_viewMode) {
                case ViewMode.Default:
                    _currGalleryPageNum = 0;
                    Image image = new() {
                        Source = await GetBitmapImage(await File.ReadAllBytesAsync(path + @"\" + _currGalleryPageNum)),
                        Width = bmGalleries[bmIdx].files[_currGalleryPageNum].width * ImageScaleSlider.Value,
                        Height = bmGalleries[bmIdx].files[_currGalleryPageNum].height * ImageScaleSlider.Value,
                    };
                    // accessing UI thread
                    ImageContainer.Children.Add(image);
                    // accessing UI thread
                    break;
                case ViewMode.Scroll:
                    Image[] images = new Image[bmGalleries[bmIdx].files.Count];

                    for (int i = 0; i < images.Length; i++) {
                        if (_ct.IsCancellationRequested) {
                            _isLoading = false;
                            return;
                        }

                        images[i] = new() {
                            Source = await GetBitmapImage(await File.ReadAllBytesAsync(path + @"\" + i.ToString())),
                            Width = bmGalleries[bmIdx].files[i].width * ImageScaleSlider.Value,
                            Height = bmGalleries[bmIdx].files[i].height * ImageScaleSlider.Value,
                        };
                    }

                    // accessing UI thread
                    for (int i = 0; i < images.Length; i++) {
                        ImageContainer.Children.Add(images[i]);
                    }
                    // accessing UI thread
                    break;
            }
            ChangeBookmarkBtnState(LoadingState.Bookmarked);
            _isLoading = false;
        }

        private static async Task<string> GetGalleryInfo(string id) {
            string address = GALLERY_INFO_DOMAIN + id + ".js";
            HttpRequestMessage galleryInfoRequest = new() {
                Method = HttpMethod.Get,
                RequestUri = new Uri(address)
            };
            HttpResponseMessage response = await _myHttpClient.SendAsync(galleryInfoRequest);
            try {
                response.EnsureSuccessStatusCode();
            } catch (HttpRequestException ex) {
                _isLoading = false;
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
            HttpResponseMessage response = await _myHttpClient.SendAsync(serverTimeRequest);
            try {
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException ex) {
                _isLoading = false;
                _mw.AlertUser("An error has occurred while getting server time. Please try again.", ex.Message);
                return null;
            }
            string responseString = await response.Content.ReadAsStringAsync();

            return Regex.Match(responseString, @"\'(.+?)/\'").Value[1..^2];
        }

        public async Task LoadImagesFromWeb(string id) {
            await PrepareImageLoad();

            try {
                string response = await GetGalleryInfo(id);
                
                if (response != null) {
                    gallery = JsonSerializer.Deserialize<Gallery>(response, serializerOptions);
                } else {
                    return;
                }

                // check if cancellation requested
                _ct.ThrowIfCancellationRequested();

                string[] imgHashArr = new string[gallery.files.Count];
                for (int i = 0; i < gallery.files.Count; i++) {
                    imgHashArr[i] = gallery.files[i].hash;
                }

                string[] imgAddresses = GetImageAddresses(imgHashArr, await GetServerTime());

                // check if cancellation requested
                _ct.ThrowIfCancellationRequested();

                byte[][] imageBytes = new byte[imgAddresses.Length][];

                for (int i = 0; i < imgAddresses.Length; i++) {
                    // check if cancellation requested
                    _ct.ThrowIfCancellationRequested();
                    foreach (string subdomain in POSSIBLE_IMAGE_SUBDOMAINS) {
                        imageBytes[i] = await GetImageBytesFromWeb(subdomain + imgAddresses[i]);
                    }
                }

                switch (_viewMode) {
                    case ViewMode.Default:
                        _currGalleryPageNum = 0;
                        Image image = new() {
                            Source = await GetBitmapImage(imageBytes[_currGalleryPageNum]),
                            Width = gallery.files[_currGalleryPageNum].width * ImageScaleSlider.Value,
                            Height = gallery.files[_currGalleryPageNum].height * ImageScaleSlider.Value
                        };
                        // accessing UI thread
                        ImageContainer.Children.Add(image);
                        // accessing UI thread
                        break;
                    case ViewMode.Scroll:
                        Image[] images = new Image[imgAddresses.Length];
                        for (int i = 0; i < imgAddresses.Length; i++) {
                            images[i] = new() {
                                Source = await GetBitmapImage(imageBytes[i]),
                                Width = gallery.files[i].width * ImageScaleSlider.Value,
                                Height = gallery.files[i].height * ImageScaleSlider.Value
                            };
                        }
                        // accessing UI thread
                        for (int i = 0; i < images.Length; i++) {
                            ImageContainer.Children.Add(images[i]);
                        }
                        // accessing UI thread
                        break;
                }

                // save gallery to local directory
                await SaveGallery(gallery.id, imageBytes);

                // check if bookmark is full
                if (bmGalleries.Count == SearchPage.MAX_BOOKMARK_PAGE * SearchPage.MAX_BOOKMARK_PER_PAGE) {
                    ChangeBookmarkBtnState(LoadingState.BookmarkFull);
                } else {
                    ChangeBookmarkBtnState(LoadingState.Loaded);
                }
            }
            catch (OperationCanceledException) {
                _isLoading = false;
                return;
            }
            _isLoading = false;
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
            HttpResponseMessage response = await _myHttpClient.SendAsync(request);
            try {
                response.EnsureSuccessStatusCode();
            } catch (HttpRequestException) {
                return null;
            }
            return await response.Content.ReadAsByteArrayAsync();
        }

        public void ChangeBookmarkBtnState(LoadingState state) {
            switch (state) {
                case LoadingState.Bookmarked:
                    BookmarkBtn.Label = "Bookmarked";
                    BookmarkBtn.IsEnabled = false;
                    break;
                case LoadingState.Loading:
                    BookmarkBtn.Label = "Loading Images...";
                    BookmarkBtn.IsEnabled = false;
                    break;
                case LoadingState.Loaded:
                    BookmarkBtn.Label = "Bookmark this Gallery";
                    BookmarkBtn.IsEnabled = true;
                    break;
                case LoadingState.BookmarkFull:
                    BookmarkBtn.Label = "Bookmark is full";
                    BookmarkBtn.IsEnabled = false;
                    break;
            }
        }
    }
}
