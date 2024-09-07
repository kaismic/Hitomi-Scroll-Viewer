using CommunityToolkit.Mvvm.ComponentModel;
using HitomiScrollViewerLib.Entities;
using HitomiScrollViewerLib.Views;
using HitomiScrollViewerLib.Views.BrowsePageViews;
using HitomiScrollViewerLib.Views.SearchPageViews;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.ApplicationModel.Resources;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using static HitomiScrollViewerLib.SharedResources;
using static HitomiScrollViewerLib.Utils;

namespace HitomiScrollViewerLib.ViewModels.SearchPageVMs {
    public partial class DownloadItemVM : ObservableObject {
        private static readonly ResourceMap _resourceMap = MainResourceMap.GetSubtree(typeof(DownloadItem).Name);

        private const string REFERER = "https://hitomi.la/";
        private const string BASE_DOMAIN = "hitomi.la";
        private const string GALLERY_INFO_DOMAIN = "https://ltn.hitomi.la/galleries/";
        private const string GALLERY_INFO_EXCLUDE_STRING = "var galleryinfo = ";
        private const string GG_JS_ADDRESS = "https://ltn.hitomi.la/gg.js";
        private const string SERVER_TIME_EXCLUDE_STRING = "0123456789/'\r\n};";

        private static readonly HttpClient HitomiHttpClient = new() {
            DefaultRequestHeaders = {
                {"referer", REFERER }
            },
            Timeout = TimeSpan.FromSeconds(15)
        };

        private enum DownloadStatus {
            Initialising,
            Paused,
            Downloading,
            Failed
        }
        [ObservableProperty]
        private DownloadStatus _currentDownloadStatus = DownloadStatus.Initialising;
        partial void OnCurrentDownloadStatusChanged(DownloadStatus value) {
            switch (value) {
                case DownloadStatus.Downloading:
                    DownloadToggleButtonSymbol = Symbol.Pause;
                    DownloadToggleButtonToolTip = _resourceMap.GetValue("ToolTipText_Pause").ValueAsString;
                    break;
                case DownloadStatus.Paused:
                    DownloadToggleButtonSymbol = Symbol.Play;
                    DownloadToggleButtonToolTip = _resourceMap.GetValue("ToolTipText_Resume").ValueAsString;
                    break;
                case DownloadStatus.Failed:
                    DownloadToggleButtonSymbol = Symbol.Refresh;
                    DownloadToggleButtonToolTip = _resourceMap.GetValue("ToolTipText_TryAgain").ValueAsString;
                    break;
            }
        }

        private CancellationTokenSource _cts;

        private Gallery _gallery;
        internal int Id { get; private set; }
        internal BookmarkItem BookmarkItem { get; set; }

        public int[] ThreadNums => Enumerable.Range(1, 8).ToArray();
        [ObservableProperty]
        private int _threadNum = 1;
        partial void OnThreadNumChanged(int value) {
            if (CurrentDownloadStatus == DownloadStatus.Downloading) {
                CancelDownloadTask(TaskCancelReason.ThreadNumChanged);
            }
        }

        [ObservableProperty]
        private string _galleryDescriptionText;
        [ObservableProperty]
        private string _progressText;
        [ObservableProperty]
        private double _progressBarValue;
        [ObservableProperty]
        private double _progressBarMaximum;

        [ObservableProperty]
        private bool _isEnabled;

        [ObservableProperty]
        private Symbol _downloadToggleButtonSymbol;
        [ObservableProperty]
        private string _downloadToggleButtonToolTip;

        private static readonly object _ggjsFetchLock = new();
        private static string _serverTime;
        private static HashSet<string> _subdomainSelectionSet;
        private static (string notContains, string contains) _subdomainOrder;

        private const int MAX_RETRY_NUM = 1;
        private const int MAX_HTTP_404_ERROR_NUM_LIMIT = 3;
        private int _retryCount;
        private static readonly List<DownloadItemVM> _waitingDownloadItemVMs = [];
        private DateTime _downloadStartTime;
        private static DateTime _lastGgjsUpdateTime;

        public event TypedEventHandler<DownloadItemVM, int> RemoveDownloadItemEvent;
        public delegate void UpdateIdEventHandler(int oldId, int newId);
        public event UpdateIdEventHandler UpdateIdEvent;

        public DownloadItemVM(int id, BookmarkItem bookmarkItem = null) {
            Id = id;
            BookmarkItem = bookmarkItem;
            GalleryDescriptionText = id.ToString();
        }

        public void StartDownload() {
            _retryCount = 0;
            _ = PreCheckDownload();
        }

        private async Task PreCheckDownload() {
            if (_retryCount > MAX_RETRY_NUM) {
                CurrentDownloadStatus = DownloadStatus.Failed;
                ProgressText = _resourceMap.GetValue("StatusText_TooManyDownloadFails").ValueAsString;
                return;
            }

            _downloadStartTime = DateTime.UtcNow;
            // ggjs previously fetched at least once
            if (_serverTime != null) {
                if (Monitor.TryEnter(_ggjsFetchLock, 0)) {
                    // not locked so immediately unlock and download
                    Monitor.Exit(_ggjsFetchLock);
                    InitDownload();
                } else {
                    // another thread is already fetching ggjs so add to download waiting list
                    lock (_waitingDownloadItemVMs) {
                        _waitingDownloadItemVMs.Add(this);
                    }
                }
                return;
            }
            // ggjs not set fetched so fetch for the first time
            await FetchAndInitDownload(false);
        }

        private async Task FetchAndInitDownload(bool alreadyLocked) {
            if (!alreadyLocked) {
                Monitor.Enter(_ggjsFetchLock);
            }
            _lastGgjsUpdateTime = DateTime.UtcNow;
            if (!await TryGetGgjsInfo()) {
                return;
            }
            Monitor.Exit(_ggjsFetchLock);
            lock (_waitingDownloadItemVMs) {
                foreach (DownloadItemVM downloadItemVM in _waitingDownloadItemVMs) {
                    downloadItemVM.InitDownload();
                }
                _waitingDownloadItemVMs.Clear();
            }
            InitDownload();
        }

        public void RemoveDownloadButton_Click(object _0, RoutedEventArgs _1) {
            CancelDownloadTask(TaskCancelReason.PausedByUser);
            RemoveSelf();
        }

        private void RemoveSelf() {
            if (BookmarkItem != null) {
                BookmarkItem.IsDownloading = false;
                BookmarkItem.EnableRemoveBtn(true);
            }
            RemoveDownloadItemEvent?.Invoke(this, Id);
        }

        public void DownloadToggleButton_Clicked(object _0, RoutedEventArgs _1) {
            switch (CurrentDownloadStatus) {
                case DownloadStatus.Downloading:
                    CancelDownloadTask(TaskCancelReason.PausedByUser);
                    break;
                case DownloadStatus.Paused or DownloadStatus.Failed:
                    StartDownload();
                    break;
            }
        }

        private enum TaskCancelReason {
            PausedByUser,
            ThreadNumChanged,
            Http404MaxLimitReached
        }
        private TaskCancelReason _taskCancelReason;

        private void CancelDownloadTask(TaskCancelReason taskCancelReason) {
            IsEnabled = false;
            _taskCancelReason = taskCancelReason;
            _cts.Cancel();
        }

        private void HandleTaskCancellation() {
            try {
                switch (_taskCancelReason) {
                    case TaskCancelReason.PausedByUser:
                        CurrentDownloadStatus = DownloadStatus.Paused;
                        ProgressText = _resourceMap.GetValue("StatusText_Paused").ValueAsString;
                        break;
                    case TaskCancelReason.ThreadNumChanged:
                        // continue download
                        _cts = new();
                        _ = TryDownload(_cts.Token);
                        break;
                    case TaskCancelReason.Http404MaxLimitReached:
                        _retryCount++;
                        if (_retryCount > MAX_RETRY_NUM) {
                            CurrentDownloadStatus = DownloadStatus.Failed;
                            ProgressText = _resourceMap.GetValue("StatusText_TooManyDownloadFails").ValueAsString;
                        } else {
                            if (_lastGgjsUpdateTime > _downloadStartTime) {
                                // ggjs is updated so try download again
                                _cts = new();
                                _ = TryDownload(_cts.Token);
                            } else {
                                if (Monitor.TryEnter(_ggjsFetchLock, 0)) {
                                    // not locked so fetch and download
                                    _ = FetchAndInitDownload(true);
                                } else {
                                    // another thread is already fetching ggjs so add to download waiting list
                                    lock (_waitingDownloadItemVMs) {
                                        _waitingDownloadItemVMs.Add(this);
                                    }
                                }
                            }
                        }
                        break;
                }
            } finally {
                IsEnabled = true;
            }
        }

        private async void InitDownload() {
            _cts = new();
            CancellationToken ct = _cts.Token;
            CurrentDownloadStatus = DownloadStatus.Downloading;
            ProgressText = "";
            BookmarkItem?.EnableRemoveBtn(false);
            if (_gallery == null) {
                ProgressText = _resourceMap.GetValue("StatusText_FetchingGalleryInfo").ValueAsString;
                string galleryInfo;
                try {
                    galleryInfo = await GetGalleryInfo(ct);
                } catch (HttpRequestException e) {
                    CurrentDownloadStatus = DownloadStatus.Failed;
                    ProgressText = _resourceMap.GetValue("StatusText_FetchingGalleryInfo_Error").ValueAsString + Environment.NewLine + e.Message;
                    return;
                } catch (TaskCanceledException) {
                    HandleTaskCancellation();
                    return;
                }

                ProgressText = _resourceMap.GetValue("StatusText_ReadingGalleryInfo").ValueAsString;
                try {
                    _gallery = JsonSerializer.Deserialize<Gallery>(galleryInfo, GALLERY_SERIALIZER_OPTIONS);
                    ProgressBarMaximum = _gallery.Files.Count;
                    GalleryDescriptionText += $" - {_gallery.Title}"; // add title to description
                } catch (JsonException e) {
                    CurrentDownloadStatus = DownloadStatus.Failed;
                    ProgressText = _resourceMap.GetValue("StatusText_ReadingGalleryInfo_Error").ValueAsString + Environment.NewLine + e.Message;
                    return;
                }
            }

            // sometimes gallery id is different to the id in ltn.hitomi.la/galleries/{id}.js but points to the same gallery
            if (Id != _gallery.Id) {
                UpdateIdEvent?.Invoke(Id, _gallery.Id);
                Id = _gallery.Id;
                GalleryDescriptionText += $"{_gallery.Id} - {_gallery.Title}";
            }

            // TODO uncomment and modify
            //if (BookmarkItem == null) {
            //    BookmarkItem = MainWindow.SearchPage.AddBookmark(_gallery);
            //}


            await TryDownload(ct);
        }

        private async Task TryDownload(CancellationToken ct) {
            _downloadStartTime = DateTime.UtcNow;
            ProgressText = _resourceMap.GetValue("StatusText_Downloading").ValueAsString;
            try {
                await DownloadImages(ct);
            } catch (TaskCanceledException) {
                HandleTaskCancellation();
                return;
            }
            HandleDownloadComplete();
        }

        private void HandleDownloadComplete(List<int> missingIndexes = null) {
            IsEnabled = false;
            missingIndexes ??= GetMissingIndexes();
            if (missingIndexes.Count > 0) {
                _retryCount++;
                // TODO
            } else {
                RemoveSelf();
            }
        }

        /**
         * <exception cref="HttpRequestException"></exception>
         * <exception cref="TaskCanceledException"></exception>
        */
        private async Task<string> GetGalleryInfo(CancellationToken ct) {
            string address = GALLERY_INFO_DOMAIN + Id + ".js";
            HttpRequestMessage galleryInfoRequest = new() {
                Method = HttpMethod.Get,
                RequestUri = new Uri(address)
            };
            HttpResponseMessage response = await HitomiHttpClient.SendAsync(galleryInfoRequest, ct);
            response.EnsureSuccessStatusCode();
            string responseString = await response.Content.ReadAsStringAsync(ct);
            return responseString[GALLERY_INFO_EXCLUDE_STRING.Length..];
        }

        private async Task<bool> TryGetGgjsInfo() {
            try {
                HttpRequestMessage request = new() {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri(GG_JS_ADDRESS)
                };

                HttpResponseMessage response = await HitomiHttpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                string ggjs = await response.Content.ReadAsStringAsync();

                _serverTime = ggjs.Substring(ggjs.Length - SERVER_TIME_EXCLUDE_STRING.Length, 10);

                string selectionSetPat = @"case (\d+)";
                MatchCollection matches = Regex.Matches(ggjs, selectionSetPat);
                _subdomainSelectionSet = matches.Select(match => match.Groups[1].Value).ToHashSet();

                string orderPat = @"var [a-z] = (\d);";
                Match match = Regex.Match(ggjs, orderPat);
                _subdomainOrder = match.Groups[1].Value == "0" ? ("aa", "ba") : ("ba", "aa");
            } catch (HttpRequestException e) {
                CurrentDownloadStatus = DownloadStatus.Failed;
                ProgressText = _resourceMap.GetValue("StatusText_FetchingServerTime_Error").ValueAsString + Environment.NewLine + e.Message;
                return false;
            }
            return true;
        }

        private static string GetImageAddress(ImageInfo imageInfo, string imageFormat) {
            string hash = imageInfo.Hash;
            string subdomainAndAddressValue = Convert.ToInt32(hash[^1..] + hash[^3..^1], 16).ToString();
            string subdomain = _subdomainSelectionSet.Contains(subdomainAndAddressValue) ? _subdomainOrder.contains : _subdomainOrder.notContains;
            return $"https://{subdomain}.{BASE_DOMAIN}/{imageFormat}/{_serverTime}/{subdomainAndAddressValue}/{hash}.{imageFormat}";
        }

        private static string GetImageFormat(ImageInfo imageInfo) {
            return imageInfo.Haswebp == 1 ? "webp" :
                   imageInfo.Hasavif == 1 ? "avif" :
                   "jxl";
        }

        private readonly struct FetchInfo {
            public int Idx { get; init; }
            public ImageInfo ImageInfo { get; init; }
            public string ImageFormat { get; init; }
            public string ImageAddress { get; init; }
        };

        /**
         * <exception cref="HttpRequestException">
         * Thrown only if <see cref="HttpRequestException.StatusCode"/> == <see cref="HttpStatusCode.NotFound"/>
         * </exception>
         * <exception cref="TaskCanceledException"></exception>
         */
        private async Task<bool> GetImage(FetchInfo fetchInfo, CancellationToken ct) {
            try {
                HttpResponseMessage response = null;
                try {
                    response = await HitomiHttpClient.GetAsync(fetchInfo.ImageAddress, ct);
                    response.EnsureSuccessStatusCode();
                } catch (HttpRequestException e) {
                    if (e.StatusCode == HttpStatusCode.NotFound) {
                        throw;
                    }
                    Debug.WriteLine(e.Message);
                    Debug.WriteLine($"Fetching {Id} at index = {fetchInfo.Idx} failed. Status Code: {e.StatusCode}");
                    return false;
                }
                try {
                    byte[] imageBytes = await response.Content.ReadAsByteArrayAsync(ct);
                    await File.WriteAllBytesAsync(
                        Path.Combine(IMAGE_DIR_V2, Id.ToString(), fetchInfo.Idx.ToString()) + '.' + fetchInfo.ImageFormat,
                        imageBytes,
                        CancellationToken.None
                    );
                    return true;
                } catch (DirectoryNotFoundException) {
                    return false;
                } catch (IOException) {
                    return false;
                }
            } catch (TaskCanceledException) {
                throw;
            }
        }

        /**
         * <exception cref="TaskCanceledException"></exception>
        */
        private Task DownloadImages(CancellationToken ct) {
            List<int> missingIndexes = GetMissingIndexes();
            ProgressBarValue = _gallery.Files.Count - missingIndexes.Count;
            FetchInfo[] fetchInfos =
                missingIndexes
                .Select(missingIndex => {
                    ImageInfo imageInfo = (_gallery.Files as List<ImageInfo>)[missingIndex];
                    string imageFormat = GetImageFormat(imageInfo);
                    return new FetchInfo() {
                        Idx = missingIndex,
                        ImageInfo = imageInfo,
                        ImageFormat = imageFormat,
                        ImageAddress = GetImageAddress(imageInfo, imageFormat)
                    };
                }).ToArray();
            /*
                example:
                fetchInfos.Length = 8, indexes = [0,1,4,5,7,9,10,11,14,15,17], concurrentTaskNum = 3
                11 / 3 = 3 r 2
                -----------------
                |3+1 | 3+1 |  3 |
                 0      7    14
                 1      9    15
                 4     10    17
                 5     11
            */
            int concurrentTaskNum = ThreadNum;
            int quotient = fetchInfos.Length / concurrentTaskNum;
            int remainder = fetchInfos.Length % concurrentTaskNum;
            Task[] tasks = new Task[concurrentTaskNum];

            int startIdx = 0;
            int http404ErrorCount = 0;
            object http404ErrorCountLock = new();
            for (int i = 0; i < concurrentTaskNum; i++) {
                int thisStartIdx = startIdx;
                int thisJMax = quotient + (i < remainder ? 1 : 0);
                tasks[i] = Task.Run(async () => {
                    for (int j = 0; j < thisJMax; j++) {
                        int k = thisStartIdx + j;
                        try {
                            bool success = await GetImage(fetchInfos[k], ct);
                            if (success) {
                                ProgressBarValue++;
                            }
                        }
                        // HttpRequestException.StatusCode should and must be HttpStatusCode.NotFound
                        catch (HttpRequestException) {
                            lock (http404ErrorCountLock) {
                                http404ErrorCount++;
                                if (http404ErrorCount > MAX_HTTP_404_ERROR_NUM_LIMIT) {
                                    CancelDownloadTask(TaskCancelReason.Http404MaxLimitReached);
                                    return;
                                }
                            }
                        }
                    }
                }, ct);
                startIdx += thisJMax;
            }
            return Task.WhenAll(tasks);
        }

        private List<int> GetMissingIndexes() {
            string imageDir = Path.Combine(IMAGE_DIR_V2, Id.ToString());
            if (!Directory.Exists(imageDir)) {
                Directory.CreateDirectory(imageDir);
                return Enumerable.Range(0, _gallery.Files.Count).ToList();
            }
            List<int> missingIndexes = [];
            for (int i = 0; i < _gallery.Files.Count; i++) {
                string[] file = Directory.GetFiles(imageDir, i.ToString() + ".*");
                if (file.Length == 0) {
                    missingIndexes.Add(i);
                }
            }
            return missingIndexes;
        }
    }
}
