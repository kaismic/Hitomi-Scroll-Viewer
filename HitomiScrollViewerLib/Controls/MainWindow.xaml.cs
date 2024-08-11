using HitomiScrollViewerLib.DbContexts;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.ApplicationModel.Resources;
using System;
using static HitomiScrollViewerLib.SharedResources;

namespace HitomiScrollViewerLib.Controls {
    public sealed partial class MainWindow : Window {
        private static readonly ResourceMap ResourceMap = MainResourceMap.GetSubtree("MainWindow");

        private static MainWindow _currentMainWindow;
        public static MainWindow CurrentMainWindow {
            get => _currentMainWindow ??= new MainWindow();
        }
        public static SearchPage SearchPage { get; private set; }
        public static ViewPage ViewPage { get; private set; }

        private MainWindow() {
            InitializeComponent();
            SearchPage = new();
            ViewPage = new();
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

            // Handle window closing
            AppWindow.Closing += async (AppWindow _, AppWindowClosingEventArgs e) => {
                if (!SearchPage.DownloadingGalleryIds.IsEmpty) {
                    ContentDialog dialog = new() {
                        DefaultButton = ContentDialogButton.Close,
                        Title = ResourceMap.GetValue("Exit_Confirm_Title_Text").ValueAsString,
                        PrimaryButtonText = TEXT_EXIT,
                        CloseButtonText = TEXT_CANCEL,
                        XamlRoot = Content.XamlRoot
                    };
                    if (await dialog.ShowAsync() == ContentDialogResult.None) {
                        e.Cancel = true;
                        return;
                    }
                }
                ViewPage.ToggleAutoScroll(false);
                ViewPage.SaveSettings();
                TagFilterSetContext.MainContext.Dispose();
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
