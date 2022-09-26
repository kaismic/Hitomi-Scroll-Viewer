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
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Storage.Streams;

namespace Hitomi_Scroll_Viewer {
    public sealed partial class ImageWatchingPage : Page {
        private readonly MainWindow _mainWindow;

        public GalleryInfo currGalleryInfo;
        public byte[][] currByteArrays;

        public bool isAutoScrolling = false;
        private static bool _is_looping = false;
        private static int _scrollSpeed;

        private double _commandBarShowRange = 0.08;

        private static readonly HttpClient _httpClient = new();
        private static readonly string IMG_INFO_BASE_DOMAIN = "https://ltn.hitomi.la/galleries/";

        private readonly DispatcherQueue _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

        public enum LoadingState {
            Bookmarked,
            Loaded,
            Loading,
            BookmarkFull
        }

        LoadingState currLoadingState = LoadingState.Loaded;

        public ImageWatchingPage(MainWindow mainWindow) {
            InitializeComponent();
            _mainWindow = mainWindow;
            Loaded += HandleInitLoad;
        }

        public void Init() {
            _mainWindow.RootFrame.KeyDown += HandleKeyDownEvent;
            MainGrid.PointerMoved += HandleMouseMovement;

            BookmarkBtn.Click += _mainWindow.searchPage.AddCurrGalleryToBookmark;
        }

        private void HandleLoopToggleBtnClick(object _, RoutedEventArgs e) {
            _is_looping = !_is_looping;
        }

        private void HandleMouseMovement(object _, PointerRoutedEventArgs args) {
            double pos = args.GetCurrentPoint(MainGrid).Position.Y;
            if (pos < _commandBarShowRange) {
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

        private void HandleInitLoad(object _, RoutedEventArgs e) {
            _commandBarShowRange *= ActualHeight;
            Loaded -= HandleInitLoad;
        }

        private void SetScrollSpeed(object sender, RangeBaseValueChangedEventArgs e) {
            _scrollSpeed = (int)(sender as Slider).Value;
        }

        private void HandleKeyDownEvent(object _, KeyRoutedEventArgs e) {
            if ((Page)_mainWindow.RootFrame.Content == this) {
                if (e.Key == Windows.System.VirtualKey.Space) {
                    if (!isAutoScrolling) {
                        Task.Run(ScrollAutomatically);
                    }
                    isAutoScrolling = !isAutoScrolling;
                }
            }
        }

        private void ScrollAutomatically() {
            while (isAutoScrolling) {
                _dispatcherQueue.TryEnqueue(() => {
                    if (MainScrollViewer.VerticalOffset != MainScrollViewer.ScrollableHeight) {
                        MainScrollViewer.ScrollToVerticalOffset(MainScrollViewer.VerticalOffset + _scrollSpeed);
                    }
                    else {
                        if (_is_looping) {
                            MainScrollViewer.ScrollToVerticalOffset(0);
                        }
                    }
                });
            }
        }

        private void ChangeImageSize(object sender, RangeBaseValueChangedEventArgs e) {
            if (ImageContainer != null) {
                for (int i = 0; i < ImageContainer.Children.Count; i++) {
                    (ImageContainer.Children[i] as Image).Width = currGalleryInfo.files[i].width * (sender as Slider).Value;
                    (ImageContainer.Children[i] as Image).Height = currGalleryInfo.files[i].height * (sender as Slider).Value;
                }
            }
        }

        public async void LoadImagesFromLocalFolder(int idx) {
            ImageContainer.Children.Clear();

            BitmapImage bmpimg;
            Image img;
            string imgStorageFolderPath = SearchPage.BM_IMGS_DIR_PATH + @"\" + _mainWindow.searchPage.bmGalleryInfo[idx].id;
            for (int i = 0; i < _mainWindow.searchPage.bmGalleryInfo[idx].files.Count; i++) {
                bmpimg = await GetImage(await File.ReadAllBytesAsync(imgStorageFolderPath + @"\" + i.ToString()));
                img = new() {
                    Source = bmpimg,
                    Width = _mainWindow.searchPage.bmGalleryInfo[idx].files[i].width * ImageSizeScaleSlider.Value,
                    Height = _mainWindow.searchPage.bmGalleryInfo[idx].files[i].height * ImageSizeScaleSlider.Value,
                };
                ImageContainer.Children.Add(img);
            }
            ChangeBookmarkBtnState(LoadingState.Bookmarked);
        }

        public async void LoadImagesFromWeb(string id) {
            ImageContainer.Children.Clear();

            string galleryInfoAddress = IMG_INFO_BASE_DOMAIN + id + ".js";
            HttpRequestMessage galleryInfoRequest = new() {
                Method = HttpMethod.Get,
                RequestUri = new Uri(galleryInfoAddress)
            };
            try {
                HttpResponseMessage response = await _httpClient.SendAsync(galleryInfoRequest);
                response.EnsureSuccessStatusCode();
                string responseString = await response.Content.ReadAsStringAsync();
                for (int i = 0; i < responseString.Length; i++) {
                    if (responseString[i] == '{') {
                        responseString = responseString[i..];
                        break;
                    }
                }
                JsonSerializerOptions serializerOptions = new() { IncludeFields = true };
                currGalleryInfo = JsonSerializer.Deserialize<GalleryInfo>(responseString, serializerOptions);
            }
            catch (Exception ex) {
                _mainWindow.AlertUser("Error. Please Try Again.", ex.Message);
                return;
            }

            string serverTimeAddress = "https://ltn.hitomi.la/gg.js";
            HttpRequestMessage serverTimeRequest = new() {
                Method = HttpMethod.Get,
                RequestUri = new Uri(serverTimeAddress)
            };
            string serverTime;
            try {
                HttpResponseMessage response = await _httpClient.SendAsync(serverTimeRequest);
                response.EnsureSuccessStatusCode();
                string responseString = await response.Content.ReadAsStringAsync();

                serverTime = Regex.Match(responseString, @"\'(.+?)/\'").Value[1..^2];
            }
            catch (Exception ex) {
                _mainWindow.AlertUser("Error. Please Try Again.", ex.Message);
                return;
            }

            string[] imgHashArr = new string[currGalleryInfo.files.Count];
            for (int i = 0; i < currGalleryInfo.files.Count; i++) {
                imgHashArr[i] = currGalleryInfo.files[i].hash;
            }

            string[] imgAddresses = GetImageAddresses(imgHashArr, serverTime);

            Image img;
            currByteArrays = new byte[imgAddresses.Length][];

            for (int i = 0; i < imgAddresses.Length; i++) {
                try {
                    currByteArrays[i] = await GetByteArray("https://aa." + imgAddresses[i]);
                    img = new() {
                        Source = await GetImage(currByteArrays[i]),
                        Width = currGalleryInfo.files[i].width * ImageSizeScaleSlider.Value,
                        Height = currGalleryInfo.files[i].height * ImageSizeScaleSlider.Value
                    };
                }
                catch (HttpRequestException e) {
                    if (e.StatusCode == System.Net.HttpStatusCode.NotFound) {
                        try {
                            currByteArrays[i] = await GetByteArray("https://ba." + imgAddresses[i]);
                            img = new() {
                                Source = await GetImage(currByteArrays[i]),
                                Width = currGalleryInfo.files[i].width * ImageSizeScaleSlider.Value,
                                Height = currGalleryInfo.files[i].height * ImageSizeScaleSlider.Value
                            };
                        }
                        catch (HttpRequestException e2) {
                            Debug.WriteLine("Message: " + e2.Message);
                            Debug.WriteLine("Status Code:" + e2.StatusCode);
                            img = new() {
                                Width = 0,
                                Height = 0,
                            };
                        }
                    } else {
                        Debug.WriteLine("Message: " + e.Message);
                        Debug.WriteLine("Status Code:" + e.StatusCode);
                        img = new() {
                            Width = 0,
                            Height = 0,
                        };
                    }
                }
                ImageContainer.Children.Add(img);
            }
            // check if bookmark is full
            if (_mainWindow.searchPage.bmGalleryInfo.Count == SearchPage.MAX_BOOKMARK_PAGE * SearchPage.MAX_BOOKMARK_PER_PAGE) {
                ChangeBookmarkBtnState(LoadingState.BookmarkFull);
            }
            // check if gallery is bookmarked
            for (int i = 0; i < _mainWindow.searchPage.bmGalleryInfo.Count; i++) {
                if (_mainWindow.searchPage.bmGalleryInfo[i].id == id) {
                    ChangeBookmarkBtnState(LoadingState.Bookmarked);
                    break;
                }
            }
            // if currLoadingState is Loading then it is neither BookmarkFull or Bookmarked so change state to Loaded
            if (currLoadingState == LoadingState.Loading) {
                ChangeBookmarkBtnState(LoadingState.Loaded);
            }
        }

        private static string[] GetImageAddresses(string[] imgHashArr, string serverTime) {
            string[] result = new string[imgHashArr.Length];
            string hash;
            string twoCharPart;
            string oneCharPart;
            //int twoCharPartInt;
            //int divisor;
            //string subdomain;
            string oneTwoCharInt;
            for (int i = 0; i < imgHashArr.Length; i++) {
                hash = imgHashArr[i];
                twoCharPart = hash[^3..^1];
                oneCharPart = hash[^1..];
                //twoCharPartInt = Convert.ToInt32(twoCharPart, 16);
                oneTwoCharInt = Convert.ToInt32(oneCharPart + twoCharPart, 16).ToString();
                //if (twoCharPartInt < 9) {
                //    divisor = 1;
                //} else if (twoCharPartInt < 48) {
                //    divisor = 2;
                //} else {
                //    divisor = 3;
                //subdomain = char.ToString((char)('a' + (twoCharPartInt % divisor)));
                //result.Add($"https://{subdomain}a.hitomi.la/webp/{serverTime}/{oneTwoCharInt}/{hash}.webp");
                result[i] = $"hitomi.la/webp/{serverTime}/{oneTwoCharInt}/{hash}.webp";
            }
            return result;
        }

        public static async Task<BitmapImage> GetImage(byte[] imgData) {
            BitmapImage img = new();

            using InMemoryRandomAccessStream stream = new();
            using (DataWriter writer = new(stream)) {
                writer.WriteBytes(imgData);
                await writer.StoreAsync();
                await writer.FlushAsync();
                writer.DetachStream();
            }

            stream.Seek(0);
            await img.SetSourceAsync(stream);

            return img;
        }

        public static async Task<byte[]> GetByteArray(string address) {
            HttpRequestMessage request = new() {
                Method = HttpMethod.Get,
                RequestUri = new Uri(address),
                Headers = {
                    {"referer", "https://hitomi.la/" }
                },
            };
            HttpResponseMessage response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsByteArrayAsync();
        }

        public void ChangeBookmarkBtnState(LoadingState state) {
            currLoadingState = state;
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
    }
}
