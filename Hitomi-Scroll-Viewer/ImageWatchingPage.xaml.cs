using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage.Streams;

namespace Hitomi_Scroll_Viewer {
    public sealed partial class ImageWatchingPage : Page {
        private readonly MainWindow _myMainWindow;

        public bool isAutoScrolling = false;
        private static bool _isLooping;
        private static int _scrollSpeed;

        private double _commandBarShowRange = 0.08;

        private static readonly HttpClient _myHttpClient = new();
        private static readonly string IMG_INFO_BASE_DOMAIN = "https://ltn.hitomi.la/galleries/";
        private static readonly string SERVER_TIME_ADDRESS = "https://ltn.hitomi.la/gg.js";
        private static readonly string REFERER = "https://hitomi.la/";
        private static readonly string[] POSSIBLE_IMAGE_SUBDOMAINS = { "https://aa.", "https://ba." };

        public CancellationTokenSource cts = new();
        public CancellationToken ct;
        public bool isLoadingImages = false;

        public enum LoadingState {
            Bookmarked,
            Loaded,
            Loading,
            BookmarkFull
        }

        public ImageWatchingPage(MainWindow mainWindow) {
            InitializeComponent();
            _myMainWindow = mainWindow;
            _isLooping = true;
            LoopBtn.IsChecked = true;
            Loaded += HandleLoad;

            ct = cts.Token;
        }

        public void Init() {
            MainGrid.PointerMoved += HandleMouseMovement;
            BookmarkBtn.Click += _myMainWindow.mySearchPage.AddBookmark;
        }

        private void HandleGoBackBtnClick(object _, RoutedEventArgs e) {
            _myMainWindow.SwitchPage();
        }

        private void HandleLoopToggleBtnClick(object _, RoutedEventArgs e) {
            _isLooping = !_isLooping;
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

        private void HandleLoad(object _, RoutedEventArgs e) {
            _commandBarShowRange *= ActualHeight;
            Loaded -= HandleLoad;
        }

        private void SetScrollSpeed(object sender, RangeBaseValueChangedEventArgs e) {
            _scrollSpeed = (int)(sender as Slider).Value;
        }

        public void HandleKeyDown(object _, KeyRoutedEventArgs e) {
            if (e.Key == Windows.System.VirtualKey.Space) {
                if (isAutoScrolling = !isAutoScrolling) {
                    Task.Run(ScrollAutomatically);
                }
            }
        }

        private void ScrollAutomatically() {
            while (isAutoScrolling) {
                DispatcherQueue.TryEnqueue(() => {
                    if (MainScrollViewer.VerticalOffset != MainScrollViewer.ScrollableHeight) {
                        MainScrollViewer.ScrollToVerticalOffset(MainScrollViewer.VerticalOffset + _scrollSpeed);
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
                Gallery gallery = GetGallery();
                double imageHeightSum = 0;
                int currPage = 0;
                for (int i = 0; i < ImageContainer.Children.Count; i++) {
                    imageHeightSum += (ImageContainer.Children[i] as Image).Height;
                    if (imageHeightSum > MainScrollViewer.VerticalOffset) {
                        currPage = i;
                        break;
                    }
                }

                imageHeightSum = 0;
                for (int i = 0; i < ImageContainer.Children.Count; i++) {
                    (ImageContainer.Children[i] as Image).Width = gallery.files[i].width * (sender as Slider).Value;
                    (ImageContainer.Children[i] as Image).Height = gallery.files[i].height * (sender as Slider).Value;
                    if (i < currPage) {
                        imageHeightSum += (ImageContainer.Children[i] as Image).Height;
                    }
                }
                // set vertical offset according to the new image size scale
                MainScrollViewer.ScrollToVerticalOffset(imageHeightSum);
            }
        }

        private async Task CheckLoadingImages() {
            if (isLoadingImages) {
                cts.Cancel();
                while (isLoadingImages) {
                    await Task.Delay(10);
                }
                cts.Dispose();
                cts = new();
                ct = cts.Token;
            }
        }

        public async Task LoadImagesFromLocalDir(int bmIdx) {
            await CheckLoadingImages();
            isLoadingImages = true;

            ImageContainer.Children.Clear();

            List<Gallery> BMGalleries = GetBMGalleries();
            Task[] tasks = new Task[BMGalleries[bmIdx].files.Count];

            string imgStorageDirPath = SearchPage.BM_IMGS_DIR_PATH + @"\" + BMGalleries[bmIdx].id;

            for (int i = 0; i < tasks.Length; i++) {
                if (ct.IsCancellationRequested) {
                    break;
                }
                Image img = new() {
                    Source = null,
                    Width = BMGalleries[bmIdx].files[i].width * ImageSizeScaleSlider.Value,
                    Height = BMGalleries[bmIdx].files[i].height * ImageSizeScaleSlider.Value,
                };

                ImageContainer.Children.Add(img);

                tasks[i] = LoadImageFromLocalDir(imgStorageDirPath + @"\" + i.ToString(), i);
            }

            // if loop finished early because of ct.IsCancellationRequested and therefore tasks array was not filled completely
            // reduce tasks array size to fit the contents
            for (int i = tasks.Length - 1; i >= 0; i--) {
                if (tasks[i] != null) {
                    tasks = tasks[0..i];
                }
            }

            await Task.WhenAll(tasks);

            if (!ct.IsCancellationRequested) {
                ChangeBookmarkBtnState(LoadingState.Bookmarked);
            }

            isLoadingImages = false;
        }

        public async Task LoadImageFromLocalDir(string path, int idx) {
            //await Task.Delay(400);
            BitmapImage bitmapImg = await GetImage(path);

            Debug.WriteLine("inserting image at " + idx);

            (ImageContainer.Children[idx] as Image).Source = bitmapImg;
        }

        public async Task LoadImagesFromWeb(string id) {
            await CheckLoadingImages();
            isLoadingImages = true;

            try {
                ImageContainer.Children.Clear();

                string galleryInfoAddress = IMG_INFO_BASE_DOMAIN + id + ".js";
                HttpRequestMessage galleryInfoRequest = new() {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri(galleryInfoAddress)
                };
                try {
                    ct.ThrowIfCancellationRequested();
                    HttpResponseMessage response = await _myHttpClient.SendAsync(galleryInfoRequest);
                    response.EnsureSuccessStatusCode();
                    string responseString = await response.Content.ReadAsStringAsync();
                    for (int i = 0; i < responseString.Length; i++) {
                        if (responseString[i] == '{') {
                            responseString = responseString[i..];
                            break;
                        }
                    }
                    JsonSerializerOptions serializerOptions = new() { IncludeFields = true };
                    SetGallery(JsonSerializer.Deserialize<Gallery>(responseString, serializerOptions));
                }
                catch (HttpRequestException ex) {
                    isLoadingImages = false;
                    _myMainWindow.AlertUser("Error. Please Try Again.", ex.Message);
                    return;
                }

                HttpRequestMessage serverTimeRequest = new() {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri(SERVER_TIME_ADDRESS)
                };
                string serverTime;
                try {
                    ct.ThrowIfCancellationRequested();
                    HttpResponseMessage response = await _myHttpClient.SendAsync(serverTimeRequest);
                    response.EnsureSuccessStatusCode();
                    string responseString = await response.Content.ReadAsStringAsync();

                    serverTime = Regex.Match(responseString, @"\'(.+?)/\'").Value[1..^2];
                }
                catch (HttpRequestException ex) {
                    isLoadingImages = false;
                    _myMainWindow.AlertUser("Error. Please Try Again.", ex.Message);
                    return;
                }

                Gallery gallery = GetGallery();
                string[] imgHashArr = new string[gallery.files.Count];
                for (int i = 0; i < gallery.files.Count; i++) {
                    imgHashArr[i] = gallery.files[i].hash;
                }

                string[] imgAddresses = GetImageAddresses(imgHashArr, serverTime);

                Image img = new() {
                    Width = 0,
                    Height = 0,
                };

                byte[][] images = new byte[imgAddresses.Length][];
                SetImages(images);

                for (int i = 0; i < imgAddresses.Length; i++) {
                    foreach (string subdomain in POSSIBLE_IMAGE_SUBDOMAINS) {
                        ct.ThrowIfCancellationRequested();
                        try {
                            images[i] = await GetByteArray(subdomain + imgAddresses[i]);
                            img = new() {
                                Source = await GetImage(images[i]),
                                Width = gallery.files[i].width * ImageSizeScaleSlider.Value,
                                Height = gallery.files[i].height * ImageSizeScaleSlider.Value
                            };
                        }
                        catch (HttpRequestException e) {
                            if (e.StatusCode == System.Net.HttpStatusCode.NotFound) {
                                continue;
                            }
                            else {
                                Debug.WriteLine("Message: " + e.Message);
                                Debug.WriteLine("Status Code:" + e.StatusCode);
                                img = new() {
                                    Width = 0,
                                    Height = 0,
                                };
                            }
                        }
                    }
                    ImageContainer.Children.Add(img);
                }
                // check if bookmark is full
                if (GetBMGalleries().Count == SearchPage.MAX_BOOKMARK_PAGE * SearchPage.MAX_BOOKMARK_PER_PAGE) {
                    ChangeBookmarkBtnState(LoadingState.BookmarkFull);
                } else {
                    ChangeBookmarkBtnState(LoadingState.Loaded);
                }
            }
            catch (OperationCanceledException) {
                isLoadingImages = false;
                return;
            }
            isLoadingImages = false;
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

        public static async Task<BitmapImage> GetImage(string path) {
            BitmapImage img = new();
            InMemoryRandomAccessStream stream = new();

            DataWriter writer = new(stream);
            writer.WriteBytes(File.ReadAllBytes(path));

            await writer.StoreAsync();
            await writer.FlushAsync();

            writer.DetachStream();

            stream.Seek(0);

            img.SetSource(stream);

            writer.Dispose();
            stream.Dispose();
            return img;
        }

        public static async Task<BitmapImage> GetImage(byte[] imgData) {
            BitmapImage img = new();
            InMemoryRandomAccessStream stream = new();

            DataWriter writer = new(stream);
            writer.WriteBytes(imgData);
            await writer.StoreAsync();
            await writer.FlushAsync();
            writer.DetachStream();
            stream.Seek(0);
            await img.SetSourceAsync(stream);

            writer.Dispose();
            stream.Dispose();
            return img;
        }

        public static async Task<byte[]> GetByteArray(string address) {
            HttpRequestMessage request = new() {
                Method = HttpMethod.Get,
                RequestUri = new Uri(address),
                Headers = {
                    {"referer", REFERER }
                },
            };
            HttpResponseMessage response = await _myHttpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

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
                default:
                    break;
            }
        }

        private Gallery GetGallery() {
            return _myMainWindow.gallery;
        }

        private void SetGallery(Gallery gallery) {
            _myMainWindow.gallery = gallery;
        }

        private List<Gallery> GetBMGalleries() {
            return _myMainWindow.BMGalleries;
        }

        private void SetImages(byte[][] images) {
            _myMainWindow.images = images;
        }
    }
}
