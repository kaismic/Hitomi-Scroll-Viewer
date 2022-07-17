using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Storage.Streams;

namespace Hitomi_Scroll_Viewer {
    public sealed partial class ImageWatchingPage : Page {
        private readonly MainWindow _mainWindow;

        public GalleryInfo currGalleryInfo;
        public List<Image> currImages;

        public bool scroll = false;
        private bool _loop = false;
        private int _scrollSpeed = 0;

        private double _commandBarShowRange = 0.08;

        private readonly HttpClient _httpClient = new();
        private readonly string _imgInfoBaseDomain = "https://ltn.hitomi.la/galleries/";

        private List<int> _imgWidths;
        private List<int> _imgHeights;

        private readonly DispatcherQueue _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

        public ImageWatchingPage(MainWindow mainWindow) {
            InitializeComponent();

            _mainWindow = mainWindow;

            _mainWindow.RootFrame.KeyDown += HandleKeyDownEvent;
            MainGrid.PointerMoved += HandleMouseMovement;
            Loaded += HandleInitLoad;

            BookmarkBtn.Click += _mainWindow.searchPage.BookmarkGallery;

            Task.Run(ScrollAutomatically);
        }

        private void HandleLoopToggleBtnClick(object _, RoutedEventArgs e) {
            _loop = !_loop;
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
                    scroll = !scroll;
                }
            }
        }

        private void ScrollAutomatically() {
            while (true) {
                if (scroll) {
                    _dispatcherQueue.TryEnqueue(() => {
                        if (MainScrollViewer.VerticalOffset != MainScrollViewer.ScrollableHeight) {
                            MainScrollViewer.ScrollToVerticalOffset(MainScrollViewer.VerticalOffset + _scrollSpeed);
                        }
                        else {
                            if (_loop) {
                                MainScrollViewer.ScrollToVerticalOffset(0);
                            }
                        }
                    });
                }
            }
        }

        private void ChangeImageSize(object sender, RangeBaseValueChangedEventArgs e) {
            if (ImageContainer != null) {
                if (ImageContainer.Children.Count > 0 && _imgWidths != null) {
                    if (_imgWidths.Count == ImageContainer.Children.Count) {
                        for (int i = 0; i < ImageContainer.Children.Count; i++) {
                            (ImageContainer.Children[i] as Image).Width = _imgWidths[i] * (sender as Slider).Value;
                            (ImageContainer.Children[i] as Image).Height = _imgHeights[i] * (sender as Slider).Value;
                        }
                    }
                }
            }
        }

        public async void ShowImages(string id) {
            ImageContainer.Children.Clear();

            string galleryInfoAddress = _imgInfoBaseDomain + id + ".js";
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

            List<string> imgHashList = new(currGalleryInfo.files.Count);
            _imgWidths = new(currGalleryInfo.files.Count);
            _imgHeights = new(currGalleryInfo.files.Count);

            for (int i = 0; i < currGalleryInfo.files.Count; i++) {
                ImageInfo imgInfo = currGalleryInfo.files[i];
                imgHashList.Add(imgInfo.hash);
                _imgWidths.Add(imgInfo.width);
                _imgHeights.Add(imgInfo.height);
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

                serverTime = Regex.Match(responseString, "\'(.+?)/\'").Value[1..^2];
            }
            catch (Exception ex) {
                _mainWindow.AlertUser("Error. Please Try Again.", ex.Message);
                return;
            }

            List<string> imgAddresses = GetImageAddresses(imgHashList, serverTime);

            Image img;
            currImages = new(imgAddresses.Count);
            int count = 0;
            foreach (string imgAddress in imgAddresses) {
                try {
                    img = new() {
                        Source = await GetImage("https://aa." + imgAddress),
                        Width = _imgWidths[count] * ImageSizeScaleSlider.Value,
                        Height = _imgHeights[count] * ImageSizeScaleSlider.Value
                    };
                }
                catch (HttpRequestException) {
                    try {
                        img = new() {
                            Source = await GetImage("https://ba." + imgAddress),
                            Width = _imgWidths[count] * ImageSizeScaleSlider.Value,
                            Height = _imgHeights[count] * ImageSizeScaleSlider.Value
                        };
                    }
                    catch (HttpRequestException e) {
                        Debug.WriteLine("Message: " + e.Message);
                        Debug.WriteLine("Status Code:" + e.StatusCode);
                        img = new() {
                            Width = 0,
                            Height = 0,
                        };
                    }
                }
                count++;
                currImages.Add(img);
                ImageContainer.Children.Add(img);
            }
            ChangeBookmarkBtnState(_mainWindow.searchPage.bookmarkedGalleryIdList.Contains(id));
        }

        private static List<string> GetImageAddresses(List<string> imgHashList, string serverTime) {
            List<string> result = new(imgHashList.Count);
            string hash;
            string twoCharPart;
            string oneCharPart;
            int twoCharPartInt;
            //int divisor;
            //string subdomain;
            string oneTwoCharInt;
            for (int i = 0; i < imgHashList.Count; i++) {
                hash = imgHashList[i];
                twoCharPart = hash[^3..^1];
                oneCharPart = hash[^1..];
                twoCharPartInt = Convert.ToInt32(twoCharPart, 16);
                oneTwoCharInt = Convert.ToInt32(oneCharPart + twoCharPart, 16).ToString();
                //if (twoCharPartInt < 9) {
                //    divisor = 1;
                //} else if (twoCharPartInt < 48) {
                //    divisor = 2;
                //} else {
                //    divisor = 3;
                //subdomain = char.ToString((char)('a' + (twoCharPartInt % divisor)));
                //result.Add($"https://{subdomain}a.hitomi.la/webp/{serverTime}/{oneTwoCharInt}/{hash}.webp");
                result.Add($"hitomi.la/webp/{serverTime}/{oneTwoCharInt}/{hash}.webp");
            }
            return result;
        }

        public async Task<BitmapImage> GetImage(string address) {
            BitmapImage img = new();
            byte[] imgData;

            HttpRequestMessage request = new() {
                Method = HttpMethod.Get,
                RequestUri = new Uri(address),
                Headers = {
                    {"referer", "https://hitomi.la/" }
                },
            };
            HttpResponseMessage response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            imgData = await response.Content.ReadAsByteArrayAsync();

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

        public void ChangeBookmarkBtnState(bool bookmarked) {
            BookmarkBtn.IsEnabled = !bookmarked;
            if (bookmarked) {
                BookmarkBtn.Label = "Bookmarked";
            }
            else {
                BookmarkBtn.Label = "Bookmark this Gallery";
            }
        }
    }
}
