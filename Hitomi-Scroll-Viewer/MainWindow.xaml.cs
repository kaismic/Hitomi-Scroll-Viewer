using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;

namespace Hitomi_Scroll_Viewer {
    public sealed partial class MainWindow : Window {
        public readonly SearchPage searchPage;
        public readonly ImageWatchingPage imageWatchingPage;
        private readonly Page[] _pages;
        private static int _currPageNum = 0;
        private readonly AppWindow _appWindow;

        public MainWindow() {
            InitializeComponent();

            Title = "Hitomi Scroll Viewer";

            IntPtr windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WindowId windowId = Win32Interop.GetWindowIdFromWindow(windowHandle);
            _appWindow = AppWindow.GetFromWindowId(windowId);
            
            searchPage = new(this);
            imageWatchingPage = new(this);
            searchPage.Init();
            imageWatchingPage.Init();
            _pages = new Page[] { searchPage, imageWatchingPage };

            RootFrame.DoubleTapped += HandleDoubleTap;
            RootFrame.Loaded += HandleInitLoad;
            Closed += HandleWindowCloseEvent;
            
            RootFrame.Content = _pages[_currPageNum];

        }

        private void HandleInitLoad(object _, RoutedEventArgs e) {
            (_appWindow.Presenter as OverlappedPresenter).Maximize();
        }

        private void HandleWindowCloseEvent(object _, WindowEventArgs args) {
            searchPage.SaveDataToFiles();
        }

        private void HandleDoubleTap(object _, DoubleTappedRoutedEventArgs args) {
            SwitchPage();
            if (RootFrame.Content as Page != imageWatchingPage) {
                imageWatchingPage.isAutoScrolling = false;
            }
        }

        public void SwitchPage() {
            _currPageNum = (_currPageNum + 1) % _pages.Length;
            RootFrame.Content = _pages[_currPageNum];
        }

        public async void AlertUser(string title, string text) {
            ContentDialog dialog = new() {
                Title = title,
                Content = text,
                CloseButtonText = "Ok",
                XamlRoot = Content.XamlRoot
            };
            await dialog.ShowAsync();
        }
    }
}
