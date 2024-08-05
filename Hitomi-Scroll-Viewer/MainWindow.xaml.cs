using Hitomi_Scroll_Viewer.MainWindowComponent;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.ApplicationModel.Resources;
using System;
using System.IO;
using static Hitomi_Scroll_Viewer.Resources;
using static Hitomi_Scroll_Viewer.Utils;

namespace Hitomi_Scroll_Viewer {
    public sealed partial class MainWindow : Window {
        private static readonly ResourceMap ResourceMap = MainResourceMap.GetSubtree("MainWindow");
        public static SearchPage SearchPage { get; private set; }
        public static ViewPage ImageWatchingPage { get; private set; }

        public MainWindow() {
            InitializeComponent();
            SearchPage = new();
            ImageWatchingPage = new();
            RootFrame.Content = SearchPage;
            SizeChanged += (object _, WindowSizeChangedEventArgs e) => {
                if (RootFrame.Content is SearchPage) {
                    SearchPage.Window_SizeChanged(e);
                } else {
                    ImageWatchingPage.Window_SizeChanged();
                }
            };
            ((OverlappedPresenter)AppWindow.Presenter).Maximize();

            Title = APP_DISPLAY_NAME;
            // create directories if they don't exist
            Directory.CreateDirectory(ROOT_DIR);
            Directory.CreateDirectory(IMAGE_DIR);

            // Handle window closing
            AppWindow.Closing += async (AppWindow _, AppWindowClosingEventArgs e) => {
                e.Cancel = true;
                if (!SearchPage.DownloadingGalleryIds.IsEmpty) {
                    ContentDialog dialog = new() {
                        DefaultButton = ContentDialogButton.Close,
                        Title = ResourceMap.GetValue("ExitConfirmText").ValueAsString,
                        PrimaryButtonText = TEXT_EXIT,
                        CloseButtonText = TEXT_CANCEL,
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
                ImageWatchingPage.ToggleAutoScroll(false);
                ImageWatchingPage.SaveSettings();
                Close();
            };
        }

        public void SwitchPage() {
            if (RootFrame.Content is ViewPage) {
                if (ImageWatchingPage.IsAutoScrolling) {
                    ImageWatchingPage.ToggleAutoScroll(false);
                }
                RootFrame.Content = SearchPage;
            } else {
                RootFrame.Content = ImageWatchingPage;
            }
        }

        private static readonly ContentDialog _notification = new() {
            CloseButtonText = TEXT_CLOSE,
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
