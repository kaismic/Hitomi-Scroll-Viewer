﻿using Microsoft.UI.Dispatching;
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
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage.Streams;

namespace Hitomi_Scroll_Viewer {
    public sealed partial class ImageWatchingPage : Page {
        private readonly MainWindow _myMainWindow;

        public GalleryInfo currGalleryInfo;
        public byte[][] currByteArrays;

        public bool isAutoScrolling = false;
        private static bool _isLooping;
        private static int _scrollSpeed;

        private double _commandBarShowRange = 0.08;

        private static readonly HttpClient _myHttpClient = new();
        private static readonly string IMG_INFO_BASE_DOMAIN = "https://ltn.hitomi.la/galleries/";
        private static readonly string[] POSSIBLE_IMAGE_SUBDOMAINS = { "https://aa.", "https://ba." };

        private readonly DispatcherQueue _myDispatcherQueue = DispatcherQueue.GetForCurrentThread();

        public CancellationTokenSource myCancellationTokenSource = new();
        public CancellationToken myCancellationToken;
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

            myCancellationToken = myCancellationTokenSource.Token;
        }

        public void Init() {
            MainGrid.PointerMoved += HandleMouseMovement;
            BookmarkBtn.Click += _myMainWindow.mySearchPage.AddCurrGalleryToBookmark;
        }

        private void HandleLoopToggleBtnClick(object _, RoutedEventArgs e) {
            _isLooping = !_isLooping;
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
                _myDispatcherQueue.TryEnqueue(() => {
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

        private void ChangeImageSize(object sender, RangeBaseValueChangedEventArgs e) {
            if (ImageContainer != null) {
                for (int i = 0; i < ImageContainer.Children.Count; i++) {
                    (ImageContainer.Children[i] as Image).Width = currGalleryInfo.files[i].width * (sender as Slider).Value;
                    (ImageContainer.Children[i] as Image).Height = currGalleryInfo.files[i].height * (sender as Slider).Value;
                }
            }
        }

        private async Task CheckLoadingImages() {
            if (isLoadingImages) {
                myCancellationTokenSource.Cancel();
                while (isLoadingImages) {
                    await Task.Delay(100);
                }
                myCancellationTokenSource.Dispose();
                myCancellationTokenSource = new();
                myCancellationToken = myCancellationTokenSource.Token;
            }
        }

        public async void LoadImagesFromLocalDir(int idx) {
            await CheckLoadingImages();
            isLoadingImages = true;

            try {
                ImageContainer.Children.Clear();

                BitmapImage bmpimg;
                Image img;
                string imgStorageDirPath = SearchPage.BM_IMGS_DIR_PATH + @"\" + _myMainWindow.mySearchPage.bmGalleryInfo[idx].id;
                for (int i = 0; i < _myMainWindow.mySearchPage.bmGalleryInfo[idx].files.Count; i++) {
                    myCancellationToken.ThrowIfCancellationRequested();
                    bmpimg = await GetImage(await File.ReadAllBytesAsync(imgStorageDirPath + @"\" + i.ToString()));
                    img = new() {
                        Source = bmpimg,
                        Width = _myMainWindow.mySearchPage.bmGalleryInfo[idx].files[i].width * ImageSizeScaleSlider.Value,
                        Height = _myMainWindow.mySearchPage.bmGalleryInfo[idx].files[i].height * ImageSizeScaleSlider.Value,
                    };
                    ImageContainer.Children.Add(img);
                }
                ChangeBookmarkBtnState(LoadingState.Bookmarked);
            } catch (OperationCanceledException) {
                isLoadingImages = false;
                return;
            }
            isLoadingImages = false;
        }

        public async void LoadImagesFromWeb(string id) {
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
                    myCancellationToken.ThrowIfCancellationRequested();
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
                    currGalleryInfo = JsonSerializer.Deserialize<GalleryInfo>(responseString, serializerOptions);
                }
                catch (HttpRequestException ex) {
                    isLoadingImages = false;
                    _myMainWindow.AlertUser("Error. Please Try Again.", ex.Message);
                    return;
                }

                string serverTimeAddress = "https://ltn.hitomi.la/gg.js";
                HttpRequestMessage serverTimeRequest = new() {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri(serverTimeAddress)
                };
                string serverTime;
                try {
                    myCancellationToken.ThrowIfCancellationRequested();
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

                string[] imgHashArr = new string[currGalleryInfo.files.Count];
                for (int i = 0; i < currGalleryInfo.files.Count; i++) {
                    imgHashArr[i] = currGalleryInfo.files[i].hash;
                }

                string[] imgAddresses = GetImageAddresses(imgHashArr, serverTime);

                Image img = new() {
                    Width = 0,
                    Height = 0,
                };
                currByteArrays = new byte[imgAddresses.Length][];

                for (int i = 0; i < imgAddresses.Length; i++) {
                    foreach (string subdomain in POSSIBLE_IMAGE_SUBDOMAINS) {
                        myCancellationToken.ThrowIfCancellationRequested();
                        try {
                            currByteArrays[i] = await GetByteArray(subdomain + imgAddresses[i]);
                            img = new() {
                                Source = await GetImage(currByteArrays[i]),
                                Width = currGalleryInfo.files[i].width * ImageSizeScaleSlider.Value,
                                Height = currGalleryInfo.files[i].height * ImageSizeScaleSlider.Value
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
                if (_myMainWindow.mySearchPage.bmGalleryInfo.Count == SearchPage.MAX_BOOKMARK_PAGE * SearchPage.MAX_BOOKMARK_PER_PAGE) {
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
            InMemoryRandomAccessStream stream = new();

            DataWriter writer = new(stream);
            writer.WriteBytes(imgData);
            await writer.StoreAsync();
            await writer.FlushAsync();
            writer.DetachStream();
            stream.Seek(0);
            await img.SetSourceAsync(stream);

            stream.Dispose();
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
    }
}
