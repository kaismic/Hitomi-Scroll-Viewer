using HitomiScrollViewerLib.Entities;
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
using static HitomiScrollViewerLib.Controls.SearchPage;
using static HitomiScrollViewerLib.SharedResources;
using static HitomiScrollViewerLib.Utils;

namespace HitomiScrollViewerLib.Controls.SearchPageComponents {
    public sealed partial class DownloadItem : Grid {
        private static readonly ResourceMap _resourceMap = MainResourceMap.GetSubtree("DownloadItem");

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
            Downloading,
            Paused,
            Failed
        }
        private DownloadStatus _downloadState;

        private CancellationTokenSource _cts = new();

        private Gallery _gallery;
        private int _id;
        internal BookmarkItem BookmarkItem;

        private readonly int[] _threadNums = Enumerable.Range(1, 8).ToArray();

        private static readonly object _ggjsFetchLock = new();
        private static bool _ggjsInitFetched = false;
        private static string _serverTime;
        private static HashSet<string> _subdomainSelectionSet;
        private static (string notContains, string contains) _subdomainOrder;

        private const int MAX_DOWNLOAD_RETRY_NUM_BY_HTTP_404 = 2;
        private const int MAX_HTTP_404_ERROR_NUM_LIMIT = 3;
        private int _retryByHttp404Count = 0;
        private static readonly List<DownloadItem> _waitingDownloadItems = [];

        public DownloadItem(int id, BookmarkItem bookmarkItem = null) {
            _id = id;
            BookmarkItem = bookmarkItem;
            InitializeComponent();
            Description.Text = id.ToString();
            InitDownload();
        }

        private void CancelDownloadButton_Click(object _0, RoutedEventArgs _1) {
            EnableButtons(false);
            _cts.Cancel();
            RemoveSelf();
        }

        private void EnableButtons(bool enable) {
            DownloadControlButton.IsEnabled = enable;
            CancelDownloadButton.IsEnabled = enable;
            ThreadNumComboBox.IsEnabled = enable;
        }

        private void RemoveSelf() {
            if (BookmarkItem != null) {
                BookmarkItem.IsDownloading = false;
                BookmarkItem.EnableRemoveBtn(true);
            }
            DownloadingGalleryIds.TryRemove(_id, out _);
            MainWindow.SearchPage.DownloadingItems.Remove(this);
        }

        private void DownloadControlButton_Clicked(object _0, RoutedEventArgs _1) {
            switch (_downloadState) {
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

        private void SetDownloadControlBtnState() {
            switch (_downloadState) {
                case DownloadStatus.Downloading:
                    DownloadControlButton.Content = new SymbolIcon(Symbol.Pause);
                    ToolTipService.SetToolTip(DownloadControlButton, _resourceMap.GetValue("ToolTipText_Pause").ValueAsString);
                    break;
                case DownloadStatus.Paused:
                    DownloadControlButton.Content = new SymbolIcon(Symbol.Play);
                    ToolTipService.SetToolTip(DownloadControlButton, _resourceMap.GetValue("ToolTipText_Resume").ValueAsString);
                    break;
                case DownloadStatus.Failed:
                    DownloadControlButton.Content = new SymbolIcon(Symbol.Refresh);
                    ToolTipService.SetToolTip(DownloadControlButton, _resourceMap.GetValue("ToolTipText_TryAgain").ValueAsString);
                    break;
            }
        }

        private void SetStateAndText(DownloadStatus state, string text) {
            _downloadState = state;
            DownloadStatusTextBlock.Text = text;
            SetDownloadControlBtnState();
        }

        private enum TaskCancelReason {
            PausedByUser,
            ThreadNumChanged,
            Http404MaxLimitReached
        }
        private TaskCancelReason _taskCancelReason;

        private void CancelDownloadTask(TaskCancelReason taskCancelReason) {
            EnableButtons(false);
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
                    }
                    // fetch ggjs and continue download
                    else if (Monitor.TryEnter(_ggjsFetchLock, 0)) {
                        try {
                            await GetGgjsInfo();
                            InitDownload();
                        } catch (HttpRequestException e) {
                            SetStateAndText(DownloadStatus.Failed, _resourceMap.GetValue("StatusText_FetchingServerTime_Error").ValueAsString + Environment.NewLine + e.Message);
                            return;
                        } finally {
                            Monitor.Exit(_ggjsFetchLock);
                        }
                    }
                    break;
            }
            EnableButtons(true);
        }

        private void ThreadNumComboBox_SelectionChanged(object _0, SelectionChangedEventArgs e) {
            // ignore initial default selection
            if (e.RemovedItems.Count == 0) {
                return;
            }
            if (_downloadState == DownloadStatus.Downloading) {
                CancelDownloadTask(TaskCancelReason.ThreadNumChanged);
            }
        }

        private async void InitDownload() {
            _cts = new();
            CancellationToken ct = _cts.Token;
            SetStateAndText(DownloadStatus.Downloading, "");
            BookmarkItem?.EnableRemoveBtn(false);
            if (_gallery == null) {
                DownloadStatusTextBlock.Text = _resourceMap.GetValue("StatusText_FetchingGalleryInfo").ValueAsString;
                string galleryInfo;
                try {
                    galleryInfo = await GetGalleryInfo(ct);
                } catch (HttpRequestException e) {
                    if (e.InnerException != null) {
                        _ = File.AppendAllTextAsync(
                            LOGS_PATH_V2,
                            '{' + Environment.NewLine +
                            $"  {_id}," + Environment.NewLine +
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

                DownloadStatusTextBlock.Text = _resourceMap.GetValue("StatusText_ReadingGalleryInfo").ValueAsString;
                try {
                    _gallery = JsonSerializer.Deserialize<Gallery>(galleryInfo, DEFAULT_SERIALIZER_OPTIONS);
                    DownloadProgressBar.Maximum = _gallery.Files.Length;
                    Description.Text += $" - {_gallery.Title}"; // add title to description
                } catch (JsonException e) {
                    SetStateAndText(DownloadStatus.Failed, _resourceMap.GetValue("StatusText_ReadingGalleryInfo_Error").ValueAsString + Environment.NewLine + e.Message);
                    return;
                }
            }

            // sometimes gallery id is different to the id in ltn.hitomi.la/galleries/{id}.js but points to the same gallery
            if (_id != _gallery.Id) {
                DownloadingGalleryIds.TryAdd(_gallery.Id, 0);
                DownloadingGalleryIds.TryRemove(_id, out _);
                _id = _gallery.Id;
                Description.Text += $"{_gallery.Id} - {_gallery.Title}";
            }

            // TODO uncomment and modify
            //if (BookmarkItem == null) {
            //    BookmarkItem = MainWindow.SearchPage.AddBookmark(_gallery);
            //}

            DownloadStatusTextBlock.Text = _resourceMap.GetValue("StatusText_CalculatingDownloadNumber").ValueAsString;
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
                missingIndexes = Enumerable.Range(0, _gallery.Files.Length).ToList();
            }
            DownloadProgressBar.Value = _gallery.Files.Length - missingIndexes.Count;
            DownloadStatusTextBlock.Text = _resourceMap.GetValue("StatusText_FetchingServerTime").ValueAsString;

            if (!_ggjsInitFetched) {
                _ggjsInitFetched = true;
                // this DownloadItem is the first DownloadItem so fetch ggjs
                // even if thread didn't acquire the lock, it means that another thread got in between this
                // code state and is fetching ggjs anyway so no problem
                if (Monitor.TryEnter(_ggjsFetchLock, 0)) {
                    try {
                        await GetGgjsInfo();
                    } catch (HttpRequestException e) {
                        SetStateAndText(DownloadStatus.Failed, _resourceMap.GetValue("StatusText_FetchingServerTime_Error").ValueAsString + Environment.NewLine + e.Message);
                        return;
                    } finally {
                        Monitor.Exit(_ggjsFetchLock);
                    }
                }
            }

            lock (_waitingDownloadItems) {
                if (Monitor.TryEnter(_ggjsFetchLock, 0)) {
                    Monitor.Exit(_ggjsFetchLock);
                } else {
                    // another thread is already fetching ggjs so add to download waiting list
                    _waitingDownloadItems.Add(this);
                    return;
                }
            }

            // at here, no thread is fetching ggjs so just start download
            DownloadStatusTextBlock.Text = _resourceMap.GetValue("StatusText_Downloading").ValueAsString;
            try {
                await DownloadImages(ct);
            } catch (TaskCanceledException) {
                HandleTaskCancellation();
                return;
            }
            FinishDownload();
        }

        private void FinishDownload(List<int> missingIndexes = null) {
            EnableButtons(false);
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
            string address = GALLERY_INFO_DOMAIN + _id + ".js";
            HttpRequestMessage galleryInfoRequest = new() {
                Method = HttpMethod.Get,
                RequestUri = new Uri(address)
            };
            HttpResponseMessage response = await HitomiHttpClient.SendAsync(galleryInfoRequest, ct);
            response.EnsureSuccessStatusCode();
            string responseString = await response.Content.ReadAsStringAsync(ct);
            return responseString[GALLERY_INFO_EXCLUDE_STRING.Length..];
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

            lock (_waitingDownloadItems) {
                foreach (var downloadItem in _waitingDownloadItems) {
                    downloadItem.InitDownload();
                }
                _waitingDownloadItems.Clear();
            }
        }

        private static string GetImageAddress(ImageInfo imageInfo, string imageFormat) {
            string hash = imageInfo.Hash;
            string subdomainAndAddressValue = Convert.ToInt32(hash[^1..] + hash[^3..^1], 16).ToString();
            string subdomain = _subdomainSelectionSet.Contains(subdomainAndAddressValue) ? _subdomainOrder.contains : _subdomainOrder.notContains;
            return $"https://{subdomain}.{BASE_DOMAIN}/{imageFormat}/{_serverTime}/{subdomainAndAddressValue}/{hash}.{imageFormat}";
        }

        private static string GetImageFormat(ImageInfo imageInfo) {
            return imageInfo.HasWebp == 1 ? "webp" :
                   imageInfo.HasAvif == 1 ? "avif" :
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
                    Debug.WriteLine($"Fetching {_id} at index = {fetchInfo.Idx} failed. Status Code: {e.StatusCode}");
                    return false;
                }
                try {
                    byte[] imageBytes = await response.Content.ReadAsByteArrayAsync(ct);
                    await File.WriteAllBytesAsync(
                        Path.Combine(IMAGE_DIR_V2, _id.ToString(), fetchInfo.Idx.ToString()) + '.' + fetchInfo.ImageFormat,
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
            Directory.CreateDirectory(Path.Combine(IMAGE_DIR_V2, _id.ToString()));
            List<int> missingIndexes = GetMissingIndexes();
            FetchInfo[] fetchInfos =
                missingIndexes
                .Select(missingIndex => {
                    ImageInfo imageInfo = _gallery.Files[missingIndex];
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
            int concurrentTaskNum = (int)ThreadNumComboBox.SelectedItem;
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
                                    lock (DownloadProgressBar) {
                                        DownloadProgressBar.Value++;
                                    }
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
            string imageDir = Path.Combine(IMAGE_DIR_V2, _id.ToString());
            List<int> missingIndexes = [];
            for (int i = 0; i < _gallery.Files.Length; i++) {
                string[] file = Directory.GetFiles(imageDir, i.ToString() + ".*");
                if (file.Length == 0) {
                    missingIndexes.Add(i);
                }
            }
            return missingIndexes;
        }
    }
}
