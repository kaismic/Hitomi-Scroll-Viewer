using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using static Hitomi_Scroll_Viewer.Utils;

namespace Hitomi_Scroll_Viewer.SearchPageComponent {
    public sealed partial class DownloadItem : Grid {
        private static readonly string DOWNLOAD_PAUSED = "Download Paused";
        private enum DownloadingState {
            Downloading,
            Paused,
            Failed
        }
        private DownloadingState _downloadingState = DownloadingState.Downloading;

        private readonly SearchPage _sp;
        private readonly HttpClient _httpClient;
        private CancellationTokenSource _cts;
        private readonly ObservableCollection<DownloadItem> _downloadingItems;

        private Gallery _gallery;
        private string _id;
        private BookmarkItem _bmItem;

        private readonly int[] _downloadThreadNums = [1, 2, 3, 4, 5, 6, 7, 8];
        private int _downloadThreadNum = 1;

        public DownloadItem(string id, HttpClient httpClient, SearchPage sp, ObservableCollection<DownloadItem> downloadingItems) {
            _id = id;
            _httpClient = httpClient;
            _sp = sp;
            _cts = new();
            _downloadingItems = downloadingItems;

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
        }

        private void RemoveSelf() {
            _sp.downloadingGalleries.TryRemove(_gallery.id, out _);
            _downloadingItems.Remove(this);
        }

        private void PauseOrResume(object _0, RoutedEventArgs _1) {
            EnableButtons(false);
            switch (_downloadingState) {
                case DownloadingState.Downloading:
                    // pause
                    _cts.Cancel();
                    break;
                case DownloadingState.Paused or DownloadingState.Failed:
                    // resume
                    _cts = new();
                    Download(_cts.Token);
                    EnableButtons(true);
                    break;
            }
        }
        private void SetDownloadControlBtnState() {
            switch (_downloadingState) {
                case DownloadingState.Downloading:
                    DownloadControlBtn.Content = new SymbolIcon(Symbol.Pause);
                    ToolTipService.SetToolTip(DownloadControlBtn, "Pause Download");
                    break;
                case DownloadingState.Paused:
                    DownloadControlBtn.Content = new SymbolIcon(Symbol.Play);
                    ToolTipService.SetToolTip(DownloadControlBtn, "Resume Download");
                    break;
                case DownloadingState.Failed:
                    DownloadControlBtn.Content = new SymbolIcon(Symbol.Refresh);
                    ToolTipService.SetToolTip(DownloadControlBtn, "Try Again");
                    break;
            }
        }

        private void SetStateAndText(DownloadingState state, string text) {
            _downloadingState = state;
            DownloadStatus.Text = text;
            SetDownloadControlBtnState();
        }

        private void SetStateAndTextToPaused() {
            SetStateAndText(DownloadingState.Paused, DOWNLOAD_PAUSED);
            EnableButtons(true);
        }

        private void HandleDownloadSuccess() {
            _bmItem.ReloadImages();
            EnableButtons(false);
            RemoveSelf();
        }

        private async void Download(CancellationToken ct) {
            SetStateAndText(DownloadingState.Downloading, "");
            if (_gallery == null) {
                DownloadStatus.Text = "Getting gallery info...";
                string galleryInfo;
                try {
                    galleryInfo = await GetGalleryInfo(_httpClient, _id, ct);
                } catch (HttpRequestException e) {
                    if (e.InnerException != null) {
                        _ = File.AppendAllTextAsync(
                            LOGS_PATH,
                            '{' + Environment.NewLine + GetExceptionDetails(e) + Environment.NewLine + '}' + Environment.NewLine,
                            ct
                        );
                    }
                    SetStateAndText(DownloadingState.Failed, "An error has occurred while getting gallery info.\n" + e.Message);
                    return;
                } catch (TaskCanceledException) {
                    SetStateAndTextToPaused();
                    return;
                }

                DownloadStatus.Text = "Reading gallery JSON file...";
                try {
                    _gallery = JsonSerializer.Deserialize<Gallery>(galleryInfo, serializerOptions);
                    DownloadProgressBar.Maximum = _gallery.files.Length;
                    Description.Text += $" - {_gallery.title}"; // add title to description
                }
                catch (JsonException e) {
                    SetStateAndText(DownloadingState.Failed, "An error has occurred while reading gallery json.\n" + e.Message);
                    return;
                }
            }

            // sometimes gallery id is different to the id in ltn.hitomi.la/galleries/{id}.js but points to the same gallery
            if (_id != _gallery.id) {
                _sp.downloadingGalleries.TryAdd(_gallery.id, 0);
                _sp.downloadingGalleries.TryRemove(_id, out _);
                _id = _gallery.id;
            }

            if (_bmItem == null) {
                _bmItem = _sp.AddBookmark(_gallery, false);
            }

            DownloadStatus.Text = "Calculating number of images to download...";
            List<int> missingIndexes;
            try {
                missingIndexes = GetMissingIndexes(_gallery);
                // no missing indexes 
                if (missingIndexes.Count == 0) {
                    HandleDownloadSuccess();
                    return;
                }
            } catch (DirectoryNotFoundException) {
                // need to download all images
                missingIndexes = Enumerable.Range(0, _gallery.files.Length).ToList();
            }
            DownloadProgressBar.Value = _gallery.files.Length - missingIndexes.Count;

            DownloadStatus.Text = "Getting server time...";
            string serverTime;
            try {
                serverTime = await GetServerTime(_httpClient, ct);
            } catch (HttpRequestException e) {
                SetStateAndText(DownloadingState.Failed, "An error has occurred while getting the server time.\n" + e.Message);
                return;
            } catch (TaskCanceledException) {
                SetStateAndTextToPaused();
                return;
            }

            DownloadStatus.Text = "Getting Image Addresses...";
            ImageInfo[] imageInfos = new ImageInfo[missingIndexes.Count];
            for (int i = 0; i < missingIndexes.Count; i++) {
                imageInfos[i] = _gallery.files[missingIndexes[i]];
            }
            string[] imgFormats = GetImageFormats(imageInfos);
            string[] imgAddresses = GetImageAddresses(imageInfos, imgFormats, serverTime);

            DownloadStatus.Text = "Downloading Images...";
            Task[] tasks = DownloadImages(
                _httpClient,
                _gallery.id,
                imgAddresses,
                imgFormats,
                missingIndexes,
                _downloadThreadNum,
                DownloadProgressBar,
                ct
            );
            Task allTask = Task.WhenAll(tasks);
            try {
                await allTask;
            } catch (TaskCanceledException) {
                SetStateAndTextToPaused();
                return;
            }
            
            if (allTask.IsCompletedSuccessfully) {
                _sp.AddBookmark(_gallery, true);
                missingIndexes = GetMissingIndexes(_gallery);
                if (missingIndexes.Count > 0) {
                    SetStateAndText(DownloadingState.Failed, $"Failed to download {missingIndexes.Count} images");
                } else {
                    HandleDownloadSuccess();
                }
            } else {
                SetStateAndText(DownloadingState.Failed, "An unknown error has occurred. Please try again");
            }
        }
    }
}
