using HitomiScrollViewerLib.Controls.Pages;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.ApplicationModel.Resources;
using System;
using System.IO;
using static HitomiScrollViewerLib.SharedResources;
using static HitomiScrollViewerLib.Utils;

namespace HitomiScrollViewerLib.Controls {
    public sealed partial class MainWindow : Window {
        private static readonly ResourceMap ResourceMap = MainResourceMap.GetSubtree("MainWindow");
        public static SearchPage SearchPage { get; private set; }
        public static ViewPage ViewPage { get; private set; }

        public MainWindow() {
            InitializeComponent();
            SearchPage = new(this);
            ViewPage = new(this);
            RootFrame.Content = SearchPage;
            SizeChanged += (object _, WindowSizeChangedEventArgs e) => {
                if (RootFrame.Content is SearchPage) {
                    SearchPage.Window_SizeChanged(e);
                } else {
                    ViewPage.Window_SizeChanged();
                }
            };
            ((OverlappedPresenter)AppWindow.Presenter).Maximize();

            Title = APP_DISPLAY_NAME;
            // create directories if they don't exist
            Directory.CreateDirectory(ROOT_DIR_V2);
            Directory.CreateDirectory(IMAGE_DIR_V2);

            // Handle window closing
            AppWindow.Closing += async (AppWindow _, AppWindowClosingEventArgs e) => {
                e.Cancel = true;
                if (!SearchPage.DownloadingGalleryIds.IsEmpty) {
                    ContentDialog dialog = new() {
                        DefaultButton = ContentDialogButton.Close,
                        Title = ResourceMap.GetValue("Exit_Confirm_Title_Text").ValueAsString,
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
                ViewPage.ToggleAutoScroll(false);
                ViewPage.SaveSettings();
                Close();
            };
        }

        public void SwitchPage() {
            if (RootFrame.Content is ViewPage) {
                if (ViewPage.IsAutoScrolling) {
                    ViewPage.ToggleAutoScroll(false);
                }
                RootFrame.Content = SearchPage;
            } else {
                RootFrame.Content = ViewPage;
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

        public async void NotifyUser(string title, string content) {
            ((TextBlock)_notification.Title).Text = title;
            ((TextBlock)_notification.Content).Text = content;
            _notification.XamlRoot = RootFrame.XamlRoot;
            await _notification.ShowAsync();
        }
    }
}
