using Microsoft.UI.Input;
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
        public static SearchPage SearchPage { get; private set; }
        public static ImageWatchingPage ImageWatchingPage { get; private set; }

        public static readonly HttpClient HitomiHttpClient = new(
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

            SearchPage = new();
            ImageWatchingPage = new();

            // Handle window closing
            AppWindow.Closing += async (AppWindow _, AppWindowClosingEventArgs e) => {
                e.Cancel = true;
                if (!SearchPage.DownloadingGalleries.IsEmpty) {
                    ContentDialog dialog = new() {
                        Title = "There are remaining downloads. Exit anyway?",
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
                File.WriteAllText(SETTINGS_PATH, JsonSerializer.Serialize(ImageWatchingPage.GetSettings(), serializerOptions));
                Close();
            };
            SizeChanged += (_, _) => { if (RootFrame.Content is ImageWatchingPage) ImageWatchingPage.Window_SizeChanged(); };
            RootFrame.PointerPressed += RootFrame_PointerPressed;

            RootFrame.Content = SearchPage;
        }

        private void RootFrame_PointerPressed(object _0, PointerRoutedEventArgs e) {
            // Get the pointer point 
            var pointerProperties = e.GetCurrentPoint(null).Properties;
            // Check if XButton1 (back button) or XButton2 (forward button) is pressed
            if (pointerProperties.PointerUpdateKind is PointerUpdateKind.XButton1Pressed or PointerUpdateKind.XButton2Pressed) {
                SwitchPage();
            }
        }

        public void SwitchPage() {
            if (RootFrame.Content is ImageWatchingPage) {
                if (ImageWatchingPage.IsAutoScrolling) {
                    ImageWatchingPage.StartStopAutoScroll(false);
                }
                RootFrame.Content = SearchPage;
            } else {
                RootFrame.Content = ImageWatchingPage;
            }
        }

        private static readonly ContentDialog _notification = new() {
            CloseButtonText = "Ok",
            Title = new TextBlock() {
                TextWrapping = TextWrapping.WrapWholeWords
            },
            Content = new TextBlock() {
                TextWrapping = TextWrapping.WrapWholeWords
            }
        };

        public static async void NotifyUser(string title, string text) {
            ((TextBlock)_notification.Title).Text = title;
            ((TextBlock)_notification.Content).Text = text;
            _notification.XamlRoot = App.MainWindow.RootFrame.XamlRoot;
            await _notification.ShowAsync();
        }
    }
}
