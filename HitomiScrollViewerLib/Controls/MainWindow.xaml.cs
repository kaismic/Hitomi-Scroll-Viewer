using HitomiScrollViewerLib.DbContexts;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.ApplicationModel.Resources;
using System;
using static HitomiScrollViewerLib.SharedResources;

namespace HitomiScrollViewerLib.Controls {
    public sealed partial class MainWindow : Window {
        private static MainWindow _currentMainWindow;
        public static MainWindow CurrentMainWindow {
            get => _currentMainWindow ??= new MainWindow();
        }
        public static SearchPage SearchPage { get; private set; }
        public static ViewPage ViewPage { get; private set; }

        private readonly IWindowSizeChangedHandler[] _windowSizeChangedHandlers;

        private readonly IAppWindowClosingHandler[] _appWindowClosingHandlers;

        private MainWindow() {
            InitializeComponent();
            SearchPage = new();
            ViewPage = new();
            _appWindowClosingHandlers = [SearchPage, ViewPage];
            _windowSizeChangedHandlers = [SearchPage, ViewPage];
            SizeChanged += MainWindow_SizeChanged;
            AppWindow.Closing += AppWindow_Closing;

            RootFrame.Content = SearchPage;
            ((OverlappedPresenter)AppWindow.Presenter).Maximize();
            Title = APP_DISPLAY_NAME;
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

        private void AppWindow_Closing(AppWindow _, AppWindowClosingEventArgs args) {
            foreach (IAppWindowClosingHandler handler in _appWindowClosingHandlers) {
                handler.HandleAppWindowClosing(args);
                if (args.Cancel) {
                    return;
                }
            }
            HitomiContext.Main.Dispose();
        }

        private void MainWindow_SizeChanged(object sender, WindowSizeChangedEventArgs args) {
            foreach (IWindowSizeChangedHandler handler in _windowSizeChangedHandlers) {
                handler.HandleWindowSizeChanged(args);
            }
        }
    }
}
