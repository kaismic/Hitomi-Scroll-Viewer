using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage.Streams;

namespace Hitomi_Scroll_Viewer {
    public sealed partial class MainWindow : Window {
        public static readonly string IMAGE_DIR = "images";

        public SearchPage sp;
        public ImageWatchingPage iwp;
        private Page[] _appPages;
        private static int _currPageNum = 0;
        private static AppWindow _myAppWindow;

        public static Gallery gallery;
        public static List<Gallery> BMGalleries;

        public static DispatcherQueue _myDispatcherQueue = DispatcherQueue.GetForCurrentThread();

        public MainWindow() {
            InitializeComponent();

            Title = "Hitomi Scroll Viewer";

            IntPtr windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WindowId windowId = Win32Interop.GetWindowIdFromWindow(windowHandle);
            _myAppWindow = AppWindow.GetFromWindowId(windowId);

            sp = new(this);
            iwp = new(this);
            _appPages = new Page[] { sp, iwp };

            sp.Init();
            iwp.Init();

            RootFrame.DoubleTapped += HandleDoubleTap;
            RootFrame.Loaded += HandleInitLoad;
            RootFrame.KeyDown += iwp.HandleKeyDown;
            Closed += HandleWindowCloseEvent;

            RootFrame.Content = _appPages[_currPageNum];
        }

        private static void HandleInitLoad(object _, RoutedEventArgs e) {
            (_myAppWindow.Presenter as OverlappedPresenter).Maximize();
        }

        private void HandleDoubleTap(object _, DoubleTappedRoutedEventArgs args) {
            if (RootFrame.Content as Page != iwp) {
                ImageWatchingPage.isAutoScrolling = false;
            }
            SwitchPage();
        }

        private void HandleWindowCloseEvent(object _, WindowEventArgs args) {
            ImageWatchingPage.isAutoScrolling = false;
            if (gallery != null) {
                if (!IsBookmarked()) {
                    DeleteGallery(gallery.id);
                }
            }
        }

        public void SwitchPage() {
            _currPageNum = (_currPageNum + 1) % _appPages.Length;
            RootFrame.Content = _appPages[_currPageNum];
        }

        public Page GetCurrPage() {
            return _appPages[_currPageNum];
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

        public static async Task<BitmapImage> GetBitmapImage(byte[] imgData) {
            BitmapImage img = new();
            InMemoryRandomAccessStream stream = new();

            DataWriter writer = new(stream);
            writer.WriteBytes(imgData);
            await writer.StoreAsync();
            await writer.FlushAsync();
            writer.DetachStream();
            stream.Seek(0);
            await img.SetSourceAsync(stream);

            writer.Dispose();
            stream.Dispose();
            return img;
        }

        public static bool IsBookmarked() {
            for (int i = 0; i < BMGalleries.Count; i++) {
                if (BMGalleries[i].id == gallery.id) {
                    return true;
                }
            }
            return false;
        }

        public static async Task SaveGallery(string id, byte[][] imageBytes) {
            string path = IMAGE_DIR + @"\" + id;
            Directory.CreateDirectory(path);

            for (int i = 0; i < imageBytes.Length; i++) {
                await File.WriteAllBytesAsync(path + @"\" + i.ToString(), imageBytes[i]);
            }
        }

        public static void DeleteGallery(string id) {
            try {
                Directory.Delete(IMAGE_DIR + @"\" + id, true);
            } catch (DirectoryNotFoundException e) {
                Debug.WriteLine(e.Message);
            }
        }

    }
}
