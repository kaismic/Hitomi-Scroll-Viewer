using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.IO;
using System.Net.Http;
using static Hitomi_Scroll_Viewer.Utils;

namespace Hitomi_Scroll_Viewer {
    public sealed partial class MainWindow : Window {
        public static SearchPage SearchPage { get; private set; }
        public static ViewPage ImageWatchingPage { get; private set; }

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
                SearchPage.WriteTagFilters();
                ImageWatchingPage.StartStopAutoScroll(false);
                ImageWatchingPage.SaveSettings();
                Close();
            };
            SizeChanged += (_, _) => { if (RootFrame.Content is ViewPage) ImageWatchingPage.Window_SizeChanged(); };

            RootFrame.Content = SearchPage;
        }

        public void SwitchPage() {
            if (RootFrame.Content is ViewPage) {
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

        public static async void NotifyUser(string title, string content) {
            ((TextBlock)_notification.Title).Text = title;
            ((TextBlock)_notification.Content).Text = content;
            _notification.XamlRoot = App.MainWindow.RootFrame.XamlRoot;
            await _notification.ShowAsync();
        }
    }
}
