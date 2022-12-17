using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.Threading.Tasks;

namespace Hitomi_Scroll_Viewer {
    public sealed partial class MainWindow : Window {
        public readonly SearchPage mySearchPage;
        public readonly ImageWatchingPage myImageWatchingPage;
        private readonly Page[] _appPages;
        private static int _currPageNum = 0;
        private readonly AppWindow _myAppWindow;

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
            Closed += HandleWindowCloseEvent;
            
            RootFrame.Content = _appPages[_currPageNum];

        }

        private void HandleInitLoad(object _, RoutedEventArgs e) {
            (_myAppWindow.Presenter as OverlappedPresenter).Maximize();
        }

        private async void HandleWindowCloseEvent(object _, WindowEventArgs args) {
            while (mySearchPage.isSavingBookmark) {
                await Task.Delay(100);
            }
            mySearchPage.SaveDataToLocalStorage();
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
