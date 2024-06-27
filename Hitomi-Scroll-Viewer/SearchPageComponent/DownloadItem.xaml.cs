using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.ApplicationModel.Resources;
using Soluling;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using static Hitomi_Scroll_Viewer.Resources;
using static Hitomi_Scroll_Viewer.SearchPage;
using static Hitomi_Scroll_Viewer.Utils;

namespace Hitomi_Scroll_Viewer.SearchPageComponent {
    public sealed partial class DownloadItem : Grid {
        private static readonly ResourceMap ResourceMap = MainResourceMap.GetSubtree("DownloadItem");

        private static readonly string REFERER = "https://hitomi.la/";
        private static readonly string BASE_DOMAIN = "hitomi.la";
        private static readonly string GALLERY_INFO_DOMAIN = "https://ltn.hitomi.la/galleries/";
        private static readonly string GALLERY_INFO_EXCLUDE_STRING = "var galleryinfo = ";
        private static readonly string GG_JS_ADDRESS = "https://ltn.hitomi.la/gg.js";
        private static readonly string SERVER_TIME_EXCLUDE_STRING = "0123456789/'\r\n};";

        private static readonly HttpClient HitomiHttpClient = new() {
            DefaultRequestHeaders = {
                {"referer", REFERER }
            },
            Timeout = TimeSpan.FromSeconds(10)
        };

        private enum DownloadStatus {
            Downloading,
            Paused,
            Failed
        }
        private DownloadStatus _downloadingState = DownloadStatus.Downloading;

        private CancellationTokenSource _cts;

        private Gallery _gallery;
        private string _id;
        private BookmarkItem _bmItem;

        private readonly int[] _threadNums = Enumerable.Range(1, 8).ToArray();

        public DownloadItem(string id) {
            _id = id;
            _cts = new();

            InitializeComponent();

            Description.Text = id;

            CancelBtn.Click += (_, _) => {
                _cts.Cancel();
                EnableButtons(false);
                RemoveSelf();
            };

            Download(_cts.Token);
        }

        private void EnableButtons(bool enable) {
            DownloadControlBtn.IsEnabled = enable;
            CancelBtn.IsEnabled = enable;
            ThreadNumComboBox.IsEnabled = enable;
        }

        private void RemoveSelf() {
            if (_bmItem != null) {
                _bmItem.IsDownloading = false;
                _bmItem?.EnableRemoveBtn(true);
            }
            DownloadingGalleries.TryRemove(_id, out _);
            MainWindow.SearchPage.DownloadingItems.Remove(this);
        }

        private void PauseOrResume(object _0, RoutedEventArgs _1) {
            EnableButtons(false);
            switch (_downloadingState) {
                case DownloadStatus.Downloading:
                    // pause
                    _cts.Cancel();
                    break;
                case DownloadStatus.Paused or DownloadStatus.Failed:
                    // resume
                    _cts = new();
                    Download(_cts.Token);
                    EnableButtons(true);
                    break;
            }
        }

        private static readonly string TOOLTIP_TEXT_PAUSE = ResourceMap.GetValue("ToolTipText_Pause").ValueAsString;
        private static readonly string TOOLTIP_TEXT_RESUME = ResourceMap.GetValue("ToolTipText_Resume").ValueAsString;
        private static readonly string TOOLTIP_TEXT_TRY_AGAIN = ResourceMap.GetValue("ToolTipText_TryAgain").ValueAsString;

        private void SetDownloadControlBtnState() {
            switch (_downloadingState) {
                case DownloadStatus.Downloading:
                    DownloadControlBtn.Content = new SymbolIcon(Symbol.Pause);
                    ToolTipService.SetToolTip(DownloadControlBtn, TOOLTIP_TEXT_PAUSE);
                    break;
                case DownloadStatus.Paused:
                    DownloadControlBtn.Content = new SymbolIcon(Symbol.Play);
                    ToolTipService.SetToolTip(DownloadControlBtn, TOOLTIP_TEXT_RESUME);
                    break;
                case DownloadStatus.Failed:
                    DownloadControlBtn.Content = new SymbolIcon(Symbol.Refresh);
                    ToolTipService.SetToolTip(DownloadControlBtn, TOOLTIP_TEXT_TRY_AGAIN);
                    break;
            }
        }

        private void SetStateAndText(DownloadStatus state, string text) {
            _downloadingState = state;
            DownloadStatusTextBlock.Text = text;
            SetDownloadControlBtnState();
        }

        private void HandleDownloadPaused() {
            SetStateAndText(DownloadStatus.Paused, STATUS_TEXT_PAUSED);
            // download paused due to ThreadNum change so continue downloading
            if (_threadNumChanged) {
                _threadNumChanged = false;
                _cts = new();
                Download(_cts.Token);
            }
            EnableButtons(true);
        }

        private bool _threadNumChanged = false;

        private void HandleThreadNumChange(object _0, SelectionChangedEventArgs e) {
            if (e.RemovedItems.Count == 0) {
                // ignore initial default selection
                return;
            }
            EnableButtons(false);
            switch (_downloadingState) {
                case DownloadStatus.Downloading:
                    // cancel downloading and continue download with the newly updated ThreadNum
                    _threadNumChanged = true;
                    _cts.Cancel();
                    break;
                case DownloadStatus.Paused or DownloadStatus.Failed:
                    // do nothing
                    EnableButtons(true);
                    break;
            }
        }

        private static readonly string STATUS_TEXT_CALCULATING_DOWNLOAD_NUMBER = ResourceMap.GetValue("StatusText_CalculatingDownloadNumber").ValueAsString;
        private static readonly string STATUS_TEXT_DOWNLOADING = ResourceMap.GetValue("StatusText_Downloading").ValueAsString;
        private static readonly string STATUS_TEXT_FAILED = ResourceMap.GetValue("StatusText_Failed").ValueAsString;
        private static readonly string STATUS_TEXT_FETCHING_GALLERY_INFO = ResourceMap.GetValue("StatusText_FetchingGalleryInfo").ValueAsString;
        private static readonly string STATUS_TEXT_FETCHING_GALLERY_INFO_ERROR = ResourceMap.GetValue("StatusText_FetchingGalleryInfo_Error").ValueAsString;
        private static readonly string STATUS_TEXT_FETCHING_SERVER_TIME = ResourceMap.GetValue("StatusText_FetchingServerTime").ValueAsString;
        private static readonly string STATUS_TEXT_FETCHING_SERVER_TIME_ERROR = ResourceMap.GetValue("StatusText_FetchingServerTime_Error").ValueAsString;
        private static readonly string STATUS_TEXT_PAUSED = ResourceMap.GetValue("StatusText_Paused").ValueAsString;
        private static readonly string STATUS_TEXT_READING_GALLERY_INFO = ResourceMap.GetValue("StatusText_ReadingGalleryInfo").ValueAsString;
        private static readonly string STATUS_TEXT_READING_GALLERY_INFO_ERROR = ResourceMap.GetValue("StatusText_ReadingGalleryInfo_Error").ValueAsString;

        private async void Download(CancellationToken ct) {
            SetStateAndText(DownloadStatus.Downloading, "");
            _bmItem?.EnableRemoveBtn(false);
            if (_gallery == null) {
                DownloadStatusTextBlock.Text = STATUS_TEXT_FETCHING_GALLERY_INFO;
                string galleryInfo;
                try {
                    galleryInfo = await GetGalleryInfo(ct);
                } catch (HttpRequestException e) {
                    if (e.InnerException != null) {
                        _ = File.AppendAllTextAsync(
                            LOGS_PATH,
                            '{' + Environment.NewLine +
                            $"  {_id}," + Environment.NewLine +
                            GetExceptionDetails(e) + Environment.NewLine +
                            "}," + Environment.NewLine,
                            ct
                        );
                    }
                    SetStateAndText(DownloadStatus.Failed, STATUS_TEXT_FETCHING_GALLERY_INFO_ERROR + Environment.NewLine + e.Message);
                    return;
                } catch (TaskCanceledException) {
                    HandleDownloadPaused();
                    return;
                }

                DownloadStatusTextBlock.Text = STATUS_TEXT_READING_GALLERY_INFO;
                try {
                    _gallery = JsonSerializer.Deserialize<Gallery>(galleryInfo, serializerOptions);
                    DownloadProgressBar.Maximum = _gallery.files.Length;
                    Description.Text += $" - {_gallery.title}"; // add title to description
                }
                catch (JsonException e) {
                    SetStateAndText(DownloadStatus.Failed, STATUS_TEXT_READING_GALLERY_INFO_ERROR + Environment.NewLine + e.Message);
                    return;
                }
            }

            // sometimes gallery id is different to the id in ltn.hitomi.la/galleries/{id}.js but points to the same gallery
            if (_id != _gallery.id) {
                DownloadingGalleries.TryAdd(_gallery.id, 0);
                DownloadingGalleries.TryRemove(_id, out _);
                _id = _gallery.id;
                Description.Text += $"{_gallery.id} - {_gallery.title}";
            }

            if (_bmItem == null) {
                _bmItem = MainWindow.SearchPage.CreateAndAddBookmark(_gallery);
            }

            DownloadStatusTextBlock.Text = STATUS_TEXT_CALCULATING_DOWNLOAD_NUMBER;
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
                missingIndexes = Enumerable.Range(0, _gallery.files.Length).ToList();
            }
            DownloadProgressBar.Value = _gallery.files.Length - missingIndexes.Count;

            DownloadStatusTextBlock.Text = STATUS_TEXT_FETCHING_SERVER_TIME;

            string ggjs;
            try {
                ggjs = await GetggjsFile(ct);
            } catch (HttpRequestException e) {
                SetStateAndText(DownloadStatus.Failed, STATUS_TEXT_FETCHING_SERVER_TIME_ERROR + Environment.NewLine + e.Message);
                return;
            } catch (TaskCanceledException) {
                HandleDownloadPaused();
                return;
            }

            ImageInfo[] imageInfos = new ImageInfo[missingIndexes.Count];
            for (int i = 0; i < missingIndexes.Count; i++) {
                imageInfos[i] = _gallery.files[missingIndexes[i]];
            }
            string[] imgFormats = GetImageFormats(imageInfos);
            string[] imgAddresses = GetImageAddresses(imageInfos, imgFormats, ggjs);

            DownloadStatusTextBlock.Text = STATUS_TEXT_DOWNLOADING;
            try {
                await DownloadImages(
                    imgAddresses,
                    imgFormats,
                    missingIndexes,
                    ct
                );
            } catch (TaskCanceledException) {
                HandleDownloadPaused();
                return;
            }

            missingIndexes = GetMissingIndexes();
            if (missingIndexes.Count > 0) {
                SetStateAndText(DownloadStatus.Failed, MultiPattern.Format(STATUS_TEXT_FAILED, missingIndexes.Count));
            } else {
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
         * <exception cref="TaskCanceledException"></exception>
        */
        private static async Task<string> GetggjsFile(CancellationToken ct) {
            HttpRequestMessage req = new() {
                Method = HttpMethod.Get,
                RequestUri = new Uri(GG_JS_ADDRESS)
            };
            HttpResponseMessage response = await HitomiHttpClient.SendAsync(req, ct);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync(ct);
        }

        private static HashSet<string> ExtractSubdomainSelectionSet(string ggjs) {
            string pat = @"case (\d+)";
            MatchCollection matches = Regex.Matches(ggjs, pat);
            return matches.Select(match => match.Groups[1].Value).ToHashSet();
        }

        private static (string notContains, string contains) ExtractSubdomainOrder(string ggjs) {
            string pat = @"var [a-z] = (\d);";
            Match match = Regex.Match(ggjs, pat);
            return match.Groups[1].Value == "0" ? ("aa", "ba") : ("ba", "aa");
        }

        private static string[] GetImageAddresses(ImageInfo[] imageInfos, string[] imgFormats, string ggjs) {
            string serverTime = ggjs.Substring(ggjs.Length - SERVER_TIME_EXCLUDE_STRING.Length, 10);
            HashSet<string> subdomainFilterSet = ExtractSubdomainSelectionSet(ggjs);
            (string notContains, string contains) = ExtractSubdomainOrder(ggjs);

            string[] result = new string[imageInfos.Length];
            for (int i = 0; i < imageInfos.Length; i++) {
                string hash = imageInfos[i].hash;
                string subdomainAndAddressValue = Convert.ToInt32(hash[^1..] + hash[^3..^1], 16).ToString();
                string subdomain = subdomainFilterSet.Contains(subdomainAndAddressValue) ? contains : notContains;
                result[i] = $"https://{subdomain}.{BASE_DOMAIN}/{imgFormats[i]}/{serverTime}/{subdomainAndAddressValue}/{hash}.{imgFormats[i]}";
            }
            return result;
        }

        private static string[] GetImageFormats(ImageInfo[] imageInfos) {
            string[] imgFormats = new string[imageInfos.Length];
            for (int i = 0; i < imgFormats.Length; i++) {
                if (imageInfos[i].haswebp == 1) {
                    imgFormats[i] = "webp";
                } else if (imageInfos[i].hasavif == 1) {
                    imgFormats[i] = "avif";
                } else if (imageInfos[i].hasjxl == 1) {
                    imgFormats[i] = "jxl";
                }
            }
            return imgFormats;
        }

        /**
         * <exception cref="TaskCanceledException"></exception>
         */
        public async Task FetchImage(string imgAddress, string imgFormat, int idx, CancellationToken ct) {
            try {
                HttpResponseMessage response = null;
                try {
                    response = await HitomiHttpClient.GetAsync(imgAddress, ct);
                    response.EnsureSuccessStatusCode();
                } catch (HttpRequestException e) {
                    Debug.WriteLine(e.Message);
                    Debug.WriteLine("Status Code: " + e.StatusCode);
                    return;
                }
                try {
                    byte[] imageBytes = await response.Content.ReadAsByteArrayAsync(ct);
                    await File.WriteAllBytesAsync(Path.Combine(IMAGE_DIR, _id, idx.ToString()) + '.' + imgFormat, imageBytes, ct);
                } catch (DirectoryNotFoundException) {
                    return;
                } catch (IOException) {
                    return;
                }
            } catch (TaskCanceledException) {
                throw;
            }
        }

        /**
         * <exception cref="TaskCanceledException"></exception>
        */
        private Task DownloadImages(string[] imgAddresses, string[] imgFormats, List<int> missingIndexes, CancellationToken ct) {
            Directory.CreateDirectory(Path.Combine(IMAGE_DIR, _id));

            /*
                example:
                imgAddresses.Length = 8, indexes = [0,1,4,5,7,9,10,11,14,15,17], concurrentTaskNum = 3
                11 / 3 = 3 r 2
                -----------------
                |3+1 | 3+1 |  3 |
                 0      7    14
                 1      9    15
                 4     10    17
                 5     11
            */
            int concurrentTaskNum = (int)ThreadNumComboBox.SelectedItem;
            int quotient = imgAddresses.Length / concurrentTaskNum;
            int remainder = imgAddresses.Length % concurrentTaskNum;
            Task[] tasks = new Task[concurrentTaskNum];

            int startIdx = 0;
            for (int i = 0; i < concurrentTaskNum; i++) {
                int thisStartIdx = startIdx;
                int thisJMax = quotient + (i < remainder ? 1 : 0);
                tasks[i] = Task.Run(async () => {
                    for (int j = 0; j < thisJMax; j++) {
                        int idx = thisStartIdx + j;
                        await FetchImage(imgAddresses[idx], imgFormats[idx], missingIndexes[idx], ct)
                            .ContinueWith(task => {
                                if (task.IsCompletedSuccessfully) {
                                    MainWindow.SearchPage.DispatcherQueue.TryEnqueue(() => {
                                        _bmItem.UpdateSingleImage(missingIndexes[idx]);
                                        lock (DownloadProgressBar) {
                                            DownloadProgressBar.Value++;
                                        }
                                    });
                                }
                            },
                            ct);
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
            string imageDir = Path.Combine(IMAGE_DIR, _id);
            List<int> missingIndexes = [];
            for (int i = 0; i < _gallery.files.Length; i++) {
                string[] file = Directory.GetFiles(imageDir, i.ToString() + ".*");
                if (file.Length == 0) {
                    missingIndexes.Add(i);
                }
            }
            return missingIndexes;
        }
    }
}
