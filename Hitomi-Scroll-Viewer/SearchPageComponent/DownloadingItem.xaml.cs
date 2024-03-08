using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using static Hitomi_Scroll_Viewer.Utils;

namespace Hitomi_Scroll_Viewer.SearchPageComponent {
    public sealed partial class DownloadingItem : Grid {
        private static readonly string DOWNLOAD_PAUSED = "Download paused";
        private enum DownloadingState {
            Downloading,
            Paused,
            Failed
        }
        private DownloadingState _downloadingState = DownloadingState.Downloading;

        private readonly SearchPage _sp;
        private readonly HttpClient _httpClient;
        private CancellationTokenSource _cts;
        private StackPanel _parent;

        private Gallery _gallery;
        private readonly string _id;

        private readonly TextBlock _statusText;
        private readonly ProgressBar _progressBar;
        private readonly Button _downloadControlBtn;
        private readonly Button _cancelBtn;

        public DownloadingItem(string id, HttpClient httpClient, SearchPage sp, StackPanel parent) {
            _id = id;
            _httpClient = httpClient;
            _sp = sp;
            _cts = new();
            _parent = parent;

            InitializeComponent();

            BorderThickness = new(1);
            Background = new SolidColorBrush(Colors.LightBlue);
            CornerRadius = new(10);
            Padding = new(10);
            ColumnDefinitions.Add(new() { Width = new GridLength(1, GridUnitType.Star) });
            ColumnDefinitions.Add(new() { Width = new GridLength(1, GridUnitType.Auto) });
            ColumnDefinitions.Add(new() { Width = new GridLength(1, GridUnitType.Auto) });
            RowDefinitions.Add(new());
            RowDefinitions.Add(new());
            RowDefinitions.Add(new());
            ColumnSpacing = 8;
            RowSpacing = 4;

            TextBlock galleryIdText = new() {
                Text = id
            };
            SetRow(galleryIdText, 0);
            SetColumn(galleryIdText, 0);
            Children.Add(galleryIdText);

            _progressBar = new() {
                Background = new SolidColorBrush(Colors.Gray),
            };
            SetRow(_progressBar, 1);
            SetColumn(_progressBar, 0);
            Children.Add(_progressBar);

            _statusText = new() {
                TextWrapping = TextWrapping.WrapWholeWords
            };
            SetRow(_statusText, 2);
            SetColumn(_statusText, 0);
            Children.Add(_statusText);

            _downloadControlBtn = new();
            SetDownloadControlBtnState();
            SetRow(_downloadControlBtn, 0);
            SetColumn(_downloadControlBtn, 1);
            SetRowSpan(_downloadControlBtn, 3);
            _downloadControlBtn.Click += PauseOrResume;

            _cancelBtn = new() {
                Content = new SymbolIcon(Symbol.Delete)
            };
            ToolTipService.SetToolTip(_cancelBtn, "Cancel Download");
            SetRow(_cancelBtn, 0);
            SetColumn(_cancelBtn, 2);
            SetRowSpan(_cancelBtn, 3);
            _cancelBtn.Click += (_, _) => {
                EnableButtons(false);
                _cts.Cancel();
                if (_gallery != null) {
                    DeleteGallery(_gallery);
                }
                RemoveSelf();
            };

            Children.Add(_downloadControlBtn);
            Children.Add(_cancelBtn);

            Download(_cts.Token);
        }

        private void EnableButtons(bool enable) {
            _downloadControlBtn.IsEnabled = enable;
            _cancelBtn.IsEnabled = enable;
        }

        private async void PauseOrResume(object _0, RoutedEventArgs _1) {
            EnableButtons(false);
            switch (_downloadingState) {
                case DownloadingState.Downloading:
                    // pause
                    _cts.Cancel();
                    while (_downloadingState == DownloadingState.Downloading) {
                        await Task.Delay(10);
                    }
                    break;
                case DownloadingState.Paused or DownloadingState.Failed:
                    // resume
                    _cts = new();
                    Download(_cts.Token);
                    break;
            }
            EnableButtons(true);
        }

        private async void Download(CancellationToken ct) {
            _downloadingState = DownloadingState.Downloading;
            if (_gallery == null) {
                _statusText.Text = "Getting gallery info...";
                string galleryInfo;
                try {
                    galleryInfo = await GetGalleryInfo(_httpClient, _id, ct);
                } catch (HttpRequestException e) {
                    _downloadingState = DownloadingState.Failed;
                    _statusText.Text = "An error has occurred while getting gallery info.\n" + e.Message;
                    SetDownloadControlBtnState();
                    return;
                } catch (TaskCanceledException) {
                    _downloadingState = DownloadingState.Paused;
                    _statusText.Text = DOWNLOAD_PAUSED;
                    SetDownloadControlBtnState();
                    return;
                }

                _statusText.Text = "Reading gallery JSON file...";
                try {
                    _gallery = JsonSerializer.Deserialize<Gallery>(galleryInfo, serializerOptions);
                    _progressBar.Maximum = _gallery.files.Length;
                }
                catch (JsonException e) {
                    _downloadingState = DownloadingState.Failed;
                    _statusText.Text = "An error has occurred while reading gallery json.\n" + e.Message;
                    SetDownloadControlBtnState();
                    return;
                }
            }

            _statusText.Text = "Getting server time...";
            string serverTime;
            try {
                serverTime = await GetServerTime(_httpClient, ct);
            } catch (HttpRequestException e) {
                _downloadingState = DownloadingState.Failed;
                _statusText.Text = "An error has occurred while getting the server time.\n" + e.Message;
                SetDownloadControlBtnState();
                return;
            } catch (TaskCanceledException) {
                _downloadingState = DownloadingState.Paused;
                _statusText.Text = DOWNLOAD_PAUSED;
                SetDownloadControlBtnState();
                return;
            }

            int[] missingIndexes;
            try {
                missingIndexes = GetMissingIndexes(_gallery);
                if (missingIndexes.Length == 0) {
                    EnableButtons(false);
                    _sp.AddBookmark(_gallery);
                    RemoveSelf();
                    return;
                }
            } catch (DirectoryNotFoundException) {
                missingIndexes = Enumerable.Range(0, _gallery.files.Length).ToArray();
            }
            _progressBar.Value = _gallery.files.Length - missingIndexes.Length;

            _statusText.Text = "Getting Image Addresses...";
            ImageInfo[] imageInfos = new ImageInfo[missingIndexes.Length];
            for (int i = 0; i < missingIndexes.Length; i++) {
                imageInfos[i] = _gallery.files[missingIndexes[i]];
            }
            string[] imgFormats = GetImageFormats(imageInfos);
            string[] imgAddresses = GetImageAddresses(imageInfos, imgFormats, serverTime);

            if (ct.IsCancellationRequested) {
                _downloadingState = DownloadingState.Paused;
                _statusText.Text = DOWNLOAD_PAUSED;
                SetDownloadControlBtnState();
                return;
            }

            _statusText.Text = "Downloading Images...";
            Task[] tasks = DownloadImages(
                _httpClient,
                _id,
                imgAddresses,
                imgFormats,
                missingIndexes,
                _sp.DownloadThreadNum,
                _progressBar,
                ct
            );
            Task allTask = Task.WhenAll(tasks);
            try {
                await allTask;
            } catch (TaskCanceledException) {
                _downloadingState = DownloadingState.Paused;
                _statusText.Text = DOWNLOAD_PAUSED;
                SetDownloadControlBtnState();
                return;
            }
            
            if (allTask.IsCompletedSuccessfully) {
                _sp.AddBookmark(_gallery);
                missingIndexes = GetMissingIndexes(_gallery);
                if (missingIndexes.Length > 0) {
                    _downloadingState = DownloadingState.Failed;
                    _statusText.Text = "Failed to download images: " + string.Join(", ", missingIndexes);
                    SetDownloadControlBtnState();
                } else {
                    EnableButtons(false);
                    RemoveSelf();
                }
            } else {
                _downloadingState = DownloadingState.Failed;
                _statusText.Text = "An unknown error has occurred. Please try again";
                SetDownloadControlBtnState();
            }
        }

        private void SetDownloadControlBtnState() {
            switch (_downloadingState) {
                case DownloadingState.Downloading:
                    _downloadControlBtn.Content = new SymbolIcon(Symbol.Pause);
                    ToolTipService.SetToolTip(_downloadControlBtn, "Pause Download");
                    break;
                case DownloadingState.Paused:
                    _downloadControlBtn.Content = new SymbolIcon(Symbol.Play);
                    ToolTipService.SetToolTip(_downloadControlBtn, "Resume Download");
                    
                    break;
                case DownloadingState.Failed:
                    _downloadControlBtn.Content = new SymbolIcon(Symbol.Refresh);
                    ToolTipService.SetToolTip(_downloadControlBtn, "Try Again");
                    break;
            }
        }

        private void RemoveSelf() {
            _parent.Children.Remove(this);
            _sp.DownloadingGalleries.TryRemove(_id, out _);
        }
    }
}
