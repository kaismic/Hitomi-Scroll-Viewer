using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Hitomi_Scroll_Viewer {
    public sealed partial class MainWindow : Window {
        public readonly SearchPage mySearchPage;
        public readonly ImageWatchingPage myImageWatchingPage;
        private readonly Page[] _appPages;
        private static int _currPageNum = 0;
        private readonly AppWindow _myAppWindow;

        public Gallery gallery;
        public List<Gallery> BMGalleries;
        public byte[][] images;

        public DispatcherQueue _myDispatcherQueue = DispatcherQueue.GetForCurrentThread();

        public MainWindow() {
            InitializeComponent();

            Title = "Hitomi Scroll Viewer";

            IntPtr windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WindowId windowId = Win32Interop.GetWindowIdFromWindow(windowHandle);
            _myAppWindow = AppWindow.GetFromWindowId(windowId);

            mySearchPage = new(this);
            myImageWatchingPage = new(this);
            mySearchPage.Init();
            myImageWatchingPage.Init();
            _appPages = new Page[] { mySearchPage, myImageWatchingPage };

            RootFrame.DoubleTapped += HandleDoubleTap;
            RootFrame.Loaded += HandleInitLoad;
            RootFrame.KeyDown += myImageWatchingPage.HandleKeyDown;
            RootFrame.Content = _appPages[_currPageNum];
        }

        private void HandleInitLoad(object _, RoutedEventArgs e) {
            (_myAppWindow.Presenter as OverlappedPresenter).Maximize();
        }

        private void HandleDoubleTap(object _, DoubleTappedRoutedEventArgs args) {
            SwitchPage();
            if (RootFrame.Content as Page != myImageWatchingPage) {
                myImageWatchingPage.isAutoScrolling = false;
            }
        }

        public void SwitchPage() {
            _currPageNum = (_currPageNum + 1) % _appPages.Length;
            RootFrame.Content = _appPages[_currPageNum];
            GC.Collect();
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
