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
        private static readonly string EXIT_CONFIRM_TEXT = ResourceMap.GetValue("ExitConfirmText").ValueAsString;

        public static SearchPage SearchPage { get; private set; }
        public static ViewPage ImageWatchingPage { get; private set; }

        public MainWindow() {
            ((OverlappedPresenter)AppWindow.Presenter).Maximize();
            InitializeComponent();
            Title = APP_DISPLAY_NAME;
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
                        Title = EXIT_CONFIRM_TEXT,
                        PrimaryButtonText = DIALOG_BUTTON_TEXT_EXIT,
                        CloseButtonText = DIALOG_BUTTON_TEXT_CANCEL,
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
                ImageWatchingPage.ToggleAutoScroll(false);
                ImageWatchingPage.SaveSettings();
                Close();
            };
            SizeChanged += (_, _) => { if (RootFrame.Content is ViewPage) ImageWatchingPage.Window_SizeChanged(); };

            RootFrame.Content = SearchPage;
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
