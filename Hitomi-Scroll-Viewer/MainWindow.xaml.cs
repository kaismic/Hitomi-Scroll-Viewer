using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using static Hitomi_Scroll_Viewer.Utils;

namespace Hitomi_Scroll_Viewer {
    public sealed partial class MainWindow : Window {
        public readonly SearchPage sp;
        private readonly ImageWatchingPage _iwp;
        private readonly Page[] _appPages;
        private static int _currPageNum = 0;

        public Gallery gallery;

        public readonly HttpClient httpClient = new(
            new SocketsHttpHandler() {
                PooledConnectionIdleTimeout = TimeSpan.FromSeconds(15)
            }
        ) {
            DefaultRequestHeaders = {
                {"referer", REFERER }
            }
        };

        public MainWindow() {
            ((OverlappedPresenter)AppWindow.Presenter).Maximize();
            InitializeComponent();
            // create directories if they don't exist
            Directory.CreateDirectory(ROOT_DIR);
            Directory.CreateDirectory(IMAGE_DIR);

            sp = new(this);
            _iwp = new(this);
            SearchPage.Init(_iwp);
            _appPages = [sp, _iwp];

            // Switch page on double click
            RootFrame.DoubleTapped += (_, _) => {
                SwitchPage();
            };

            // Handle window closing
            AppWindow.Closing += async (AppWindow _, AppWindowClosingEventArgs e) => {
                e.Cancel = true;
                if (!sp.downloadingGalleries.IsEmpty || _iwp.IsBusy()) {
                    ContentDialog dialog = new() {
                        Title = "App is busy downloading galleries. Exit anyway?",
                        PrimaryButtonText = "Exit",
                        CloseButtonText = "Cancel",
                        XamlRoot = Content.XamlRoot
                    };
                    ContentDialogResult cdr = await dialog.ShowAsync();
                    switch (cdr) {
                        // cancel exit
                        case ContentDialogResult.None: {
                            return;
                        }
                    }
                }
                // exit app
                if (gallery != null) {
                    if (SearchPage.GetBookmarkItem(gallery.id) == null) {
                        DeleteGallery(gallery);
                    }
                }
                File.WriteAllText(SETTINGS_PATH, JsonSerializer.Serialize(_iwp.GetSettings(), serializerOptions));
                Close();
            };

            RootFrame.KeyDown += (object _, KeyRoutedEventArgs e) => {
                if (_currPageNum == 1) _iwp.HandleKeyDown(_, e);
            };

            RootFrame.Content = _appPages[_currPageNum];
        }

        public void SwitchPage() {
            if (_currPageNum == 1 && _iwp.IsAutoScrolling) _iwp.StartStopAutoScroll(false);
            _currPageNum = (_currPageNum + 1) % _appPages.Length;
            RootFrame.Content = _appPages[_currPageNum];
        }

        private readonly ContentDialog _notification = new() {
            CloseButtonText = "Ok",
            Title = new TextBlock() {
                TextWrapping = TextWrapping.WrapWholeWords
            },
            Content = new TextBlock() {
                TextWrapping = TextWrapping.WrapWholeWords
            }
        };

        public async void AlertUser(string title, string text) {
            ((TextBlock)_notification.Title).Text = title;
            ((TextBlock)_notification.Content).Text = text;
            _notification.XamlRoot = Content.XamlRoot;
            await _notification.ShowAsync();
        }
    }
}
