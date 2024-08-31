using CommunityToolkit.Mvvm.ComponentModel;
using HitomiScrollViewerLib.Entities;
using HitomiScrollViewerLib.Views.BrowsePage;
using HitomiScrollViewerLib.Views.SearchPage;
using HitomiScrollViewerLib.Windows;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.ApplicationModel.Resources;
using Soluling;
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


namespace HitomiScrollViewerLib.ViewModels.SearchPage {
    public partial class DownloadItemVM : ObservableObject{
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

        [ObservableProperty]
        private string _currentVisualState;

        private enum DownloadStatus {
            Downloading,
            Paused,
            Failed
        }
        [ObservableProperty]
        private DownloadStatus _downloadState;
        partial void OnDownloadStateChanged(DownloadStatus value) {
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

        private CancellationTokenSource _cts = new();

        private Gallery _gallery;
        internal int Id { get; private set; }
        internal BookmarkItem BookmarkItem { get; set; }

        public int[] ThreadNums => Enumerable.Range(1, 8).ToArray();
        [ObservableProperty]
        private int _threadNum = 1;

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
        private static bool _ggjsInitFetched = false;
        private static string _serverTime;
        private static HashSet<string> _subdomainSelectionSet;
        private static (string notContains, string contains) _subdomainOrder;

        private const int MAX_DOWNLOAD_RETRY_NUM_BY_HTTP_404 = 2;
        private const int MAX_HTTP_404_ERROR_NUM_LIMIT = 3;
        private int _retryByHttp404Count = 0;
        private static readonly List<DownloadItemVM> _waitingDownloadItemVMs = [];

        public event TypedEventHandler<DownloadItemVM, int> RemoveDownloadItemEvent;
        public delegate void UpdateIdEventHandler(int oldId, int newId);
        public event UpdateIdEventHandler UpdateIdEvent;

        public DownloadItemVM(int id, BookmarkItem bookmarkItem = null) {
            Id = id;
            BookmarkItem = bookmarkItem;
            GalleryDescriptionText = id.ToString();
        }

        public void CancelDownloadButton_Click(object _0, RoutedEventArgs _1) {
            IsEnabled = false;
            _cts.Cancel();
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
            switch (DownloadState) {
                case DownloadStatus.Downloading:
                    CancelDownloadTask(TaskCancelReason.PausedByUser);
                    break;
                case DownloadStatus.Paused or DownloadStatus.Failed:
                    // reset retry by http 404 error count
                    _retryByHttp404Count = 0;
                    InitDownload();
                    break;
            }
        }

        private void SetStateAndText(DownloadStatus state, string text) {
            DownloadState = state;
            ProgressText = text;
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

        private async void HandleTaskCancellation() {
            switch (_taskCancelReason) {
                case TaskCancelReason.PausedByUser:
                    SetStateAndText(DownloadStatus.Paused, _resourceMap.GetValue("StatusText_Paused").ValueAsString);
                    break;
                case TaskCancelReason.ThreadNumChanged:
                    // continue download
                    InitDownload();
                    break;
                case TaskCancelReason.Http404MaxLimitReached:
                    _retryByHttp404Count++;
                    if (_retryByHttp404Count >= MAX_DOWNLOAD_RETRY_NUM_BY_HTTP_404) {
                        SetStateAndText(DownloadStatus.Failed, _resourceMap.GetValue("StatusText_Unknown_Error").ValueAsString);
                    } else {
                        await TryGetGgjsInfo(true);
                    }
                    break;
            }
            IsEnabled = true;
        }

        private void ThreadNumComboBox_SelectionChanged(object _0, SelectionChangedEventArgs e) {
            // ignore initial default selection
            if (e.RemovedItems.Count == 0) {
                return;
            }
            if (DownloadState == DownloadStatus.Downloading) {
                CancelDownloadTask(TaskCancelReason.ThreadNumChanged);
            }
        }

        internal async void InitDownload() {
            _cts = new();
            CancellationToken ct = _cts.Token;
            SetStateAndText(DownloadStatus.Downloading, "");
            BookmarkItem?.EnableRemoveBtn(false);
            if (_gallery == null) {
                ProgressText = _resourceMap.GetValue("StatusText_FetchingGalleryInfo").ValueAsString;
                string galleryInfo;
                try {
                    galleryInfo = await GetGalleryInfo(ct);
                } catch (HttpRequestException e) {
                    if (e.InnerException != null) {
                        _ = File.AppendAllTextAsync(
                            LOGS_PATH_V2,
                            '{' + Environment.NewLine +
                            $"  {Id}," + Environment.NewLine +
                            GetExceptionDetails(e) + Environment.NewLine +
                            "}," + Environment.NewLine,
                            ct
                        );
                    }
                    SetStateAndText(DownloadStatus.Failed, _resourceMap.GetValue("StatusText_FetchingGalleryInfo_Error").ValueAsString + Environment.NewLine + e.Message);
                    return;
                } catch (TaskCanceledException) {
                    HandleTaskCancellation();
                    return;
                }

                ProgressText = _resourceMap.GetValue("StatusText_ReadingGalleryInfo").ValueAsString;
                try {
                    _gallery = JsonSerializer.Deserialize<Gallery>(galleryInfo, DEFAULT_SERIALIZER_OPTIONS);
                    ProgressBarMaximum = _gallery.Files.Count;
                    GalleryDescriptionText += $" - {_gallery.Title}"; // add title to description
                } catch (JsonException e) {
                    SetStateAndText(DownloadStatus.Failed, _resourceMap.GetValue("StatusText_ReadingGalleryInfo_Error").ValueAsString + Environment.NewLine + e.Message);
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

            ProgressText = _resourceMap.GetValue("StatusText_CalculatingDownloadNumber").ValueAsString;
            List<int> missingIndexes;
            try {
                missingIndexes = GetMissingIndexes();
                // no missing indexes 
                if (missingIndexes.Count == 0) {
                    RemoveSelf();
                    return;
                }
            } catch (DirectoryNotFoundException) {
                // need to download all images
                missingIndexes = Enumerable.Range(0, _gallery.Files.Count).ToList();
            }
            ProgressBarValue = _gallery.Files.Count - missingIndexes.Count;
            ProgressText = _resourceMap.GetValue("StatusText_FetchingServerTime").ValueAsString;

            if (!_ggjsInitFetched) {
                // this DownloadItem is the first DownloadItem so fetch ggjs
                // even if this thread didn't acquire the lock, it means that another thread got in between
                // and that thread is going to fetch ggjs so no problem
                _ggjsInitFetched = true;
                await TryGetGgjsInfo(false);
            }

            lock (_waitingDownloadItemVMs) {
                if (Monitor.TryEnter(_ggjsFetchLock, 0)) {
                    Monitor.Exit(_ggjsFetchLock);
                } else {
                    // another thread is already fetching ggjs so add to download waiting list
                    _waitingDownloadItemVMs.Add(this);
                    return;
                }
            }

            // at here, no thread is fetching ggjs so just start download
            ProgressText = _resourceMap.GetValue("StatusText_Downloading").ValueAsString;
            try {
                await DownloadImages(ct);
            } catch (TaskCanceledException) {
                HandleTaskCancellation();
                return;
            }
            FinishDownload();
        }

        private void FinishDownload(List<int> missingIndexes = null) {
            IsEnabled = false;
            missingIndexes ??= GetMissingIndexes();
            if (missingIndexes.Count > 0) {
                SetStateAndText(DownloadStatus.Failed, MultiPattern.Format(_resourceMap.GetValue("StatusText_Failed").ValueAsString, missingIndexes.Count));
            } else {
                if (!_cts.IsCancellationRequested) {
                    _cts.Dispose();
                }
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

        private async Task TryGetGgjsInfo(bool startSelfDownload) {
            bool ggjsFetchLockTaken = false;
            try {
                Monitor.TryEnter(_ggjsFetchLock, 0, ref ggjsFetchLockTaken);
                if (ggjsFetchLockTaken) {
                    await GetGgjsInfo();
                }
            } catch (HttpRequestException e) {
                SetStateAndText(DownloadStatus.Failed, _resourceMap.GetValue("StatusText_FetchingServerTime_Error").ValueAsString + Environment.NewLine + e.Message);
                return;
            } finally {
                if (ggjsFetchLockTaken) {
                    Monitor.Exit(ggjsFetchLockTaken);
                }
            }
            if (startSelfDownload) {
                InitDownload();
            }
            lock (_waitingDownloadItemVMs) {
                foreach (var downloadItem in _waitingDownloadItemVMs) {
                    downloadItem.InitDownload();
                }
                _waitingDownloadItemVMs.Clear();
            }
        }

        /**
         * <exception cref="HttpRequestException"></exception>
        */
        private static async Task GetGgjsInfo() {
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
            Directory.CreateDirectory(Path.Combine(IMAGE_DIR_V2, Id.ToString()));
            List<int> missingIndexes = GetMissingIndexes();
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
                                MainWindow.SearchPage.DispatcherQueue.TryEnqueue(() => {
                                    BookmarkItem.UpdateSingleImage(missingIndexes[k]);
                                    ProgressBarValue++;
                                });
                            }
                        }
                        // HttpRequestException.StatusCode should and must be HttpStatusCode.NotFound
                        catch (HttpRequestException) {
                            lock (http404ErrorCountLock) {
                                http404ErrorCount++;
                                if (http404ErrorCount >= MAX_HTTP_404_ERROR_NUM_LIMIT) {
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

        /**
         * <returns>The image indexes if the image directory exists, otherwise, throws <c>DirectoryNotFoundException</c></returns>
         * <exception cref="DirectoryNotFoundException"></exception>
         */
        private List<int> GetMissingIndexes() {
            string imageDir = Path.Combine(IMAGE_DIR_V2, Id.ToString());
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
