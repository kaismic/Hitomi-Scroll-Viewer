using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using static Hitomi_Scroll_Viewer.Utils;
using static Hitomi_Scroll_Viewer.SearchPage;
using static Hitomi_Scroll_Viewer.MainWindow;
using Windows.ApplicationModel.Resources;

namespace Hitomi_Scroll_Viewer.SearchPageComponent {
    public sealed partial class DownloadItem : Grid {
        private static readonly ResourceLoader resourceLoader = ResourceLoader.GetForCurrentView("SearchPageComponent/DownloadItem");
        private static readonly string STATUS_TEXT_PAUSED = resourceLoader.GetString("StatusText_Paused");
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
                _bmItem.isDownloading = false;
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
        private void SetDownloadControlBtnState() {
            switch (_downloadingState) {
                case DownloadStatus.Downloading:
                    DownloadControlBtn.Content = new SymbolIcon(Symbol.Pause);
                    ToolTipService.SetToolTip(DownloadControlBtn, "Pause Download");
                    break;
                case DownloadStatus.Paused:
                    DownloadControlBtn.Content = new SymbolIcon(Symbol.Play);
                    ToolTipService.SetToolTip(DownloadControlBtn, "Resume Download");
                    break;
                case DownloadStatus.Failed:
                    DownloadControlBtn.Content = new SymbolIcon(Symbol.Refresh);
                    ToolTipService.SetToolTip(DownloadControlBtn, "Try Again");
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

        private async void Download(CancellationToken ct) {
            SetStateAndText(DownloadStatus.Downloading, "");
            _bmItem?.EnableRemoveBtn(false);
            if (_gallery == null) {
                DownloadStatusTextBlock.Text = "Getting gallery info...";
                string galleryInfo;
                try {
                    galleryInfo = await GetGalleryInfo(HitomiHttpClient, _id, ct);
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
                    SetStateAndText(DownloadStatus.Failed, "An error has occurred while getting gallery info.\n" + e.Message);
                    return;
                } catch (TaskCanceledException) {
                    HandleDownloadPaused();
                    return;
                }

                DownloadStatusTextBlock.Text = "Reading gallery JSON file...";
                try {
                    _gallery = JsonSerializer.Deserialize<Gallery>(galleryInfo, serializerOptions);
                    DownloadProgressBar.Maximum = _gallery.files.Length;
                    Description.Text += $" - {_gallery.title}"; // add title to description
                }
                catch (JsonException e) {
                    SetStateAndText(DownloadStatus.Failed, "An error has occurred while reading gallery json.\n" + e.Message);
                    return;
                }
            }

            // sometimes gallery id is different to the id in ltn.hitomi.la/galleries/{id}.js but points to the same gallery
            if (_id != _gallery.id) {
                DownloadingGalleries.TryAdd(_gallery.id, 0);
                DownloadingGalleries.TryRemove(_id, out _);
                _id = _gallery.id;
            }

            if (_bmItem == null) {
                _bmItem = MainWindow.SearchPage.CreateAndAddBookmark(_gallery);
            }

            DownloadStatusTextBlock.Text = "Calculating number of images to download...";
            List<int> missingIndexes;
            try {
                missingIndexes = GetMissingIndexes(_gallery);
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

            DownloadStatusTextBlock.Text = "Getting server time...";

            string ggjs;
            try {
                ggjs = await GetggjsFile(HitomiHttpClient, ct);
            } catch (HttpRequestException e) {
                SetStateAndText(DownloadStatus.Failed, "An error has occurred while getting the server time.\n" + e.Message);
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

            DownloadStatusTextBlock.Text = "Downloading Images...";
            Task downloadTask = DownloadImages(
                new DownloadInfo {
                    httpClient = HitomiHttpClient,
                    id = _gallery.id,
                    concurrentTaskNum = (int)ThreadNumComboBox.SelectedItem,
                    progressBar = DownloadProgressBar,
                    bmItem = _bmItem,
                    ct = ct
                },
                imgAddresses,
                imgFormats,
                missingIndexes
            );
            try {
                await downloadTask;
            } catch (TaskCanceledException) {
                HandleDownloadPaused();
                return;
            }

            missingIndexes = GetMissingIndexes(_gallery);
            if (missingIndexes.Count > 0) {
                SetStateAndText(DownloadStatus.Failed, $"Failed to download {missingIndexes.Count} images");
            } else {
                RemoveSelf();
            }
        }
    }
}
