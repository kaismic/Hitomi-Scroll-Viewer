using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using static Hitomi_Scroll_Viewer.Utils;

namespace Hitomi_Scroll_Viewer {
    public sealed partial class MainWindow : Window {
        private readonly SearchPage _sp;
        private readonly ImageWatchingPage _iwp;

        public Gallery CurrLoadedGallery { get; set; }

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

            _sp = new(this);
            _iwp = new(this);

            // Switch page on double click
            RootFrame.DoubleTapped += (_, _) => SwitchPage();

            // Handle window closing
            AppWindow.Closing += async (AppWindow _, AppWindowClosingEventArgs e) => {
                e.Cancel = true;
                if (!_sp.downloadingGalleries.IsEmpty) {
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
                // save settings and exit app
                File.WriteAllText(SETTINGS_PATH, JsonSerializer.Serialize(_iwp.GetSettings(), serializerOptions));
                Close();
            };

            RootFrame.KeyDown += (object _, KeyRoutedEventArgs e) => {
                if (RootFrame.Content is ImageWatchingPage) _iwp.HandleKeyDown(_, e);
            };

            RootFrame.Content = _sp;
        }

        public void SwitchPage() {
            if (RootFrame.Content is ImageWatchingPage) {
                if (_iwp.IsAutoScrolling) {
                    _iwp.StartStopAutoScroll(false);
                }
                RootFrame.Content = _sp;
            } else {
                RootFrame.Content = _iwp;
            }
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

        public async void NotifyUser(string title, string text) {
            ((TextBlock)_notification.Title).Text = title;
            ((TextBlock)_notification.Content).Text = text;
            _notification.XamlRoot = Content.XamlRoot;
            await _notification.ShowAsync();
        }

        public void LoadGallery(Gallery gallery) {
            _iwp.LoadGalleryFromLocalDir(gallery);
        }
    }
}
