using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Hitomi_Scroll_Viewer {
    public sealed partial class MainWindow : Window {
        public static readonly string ROOT_DIR = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\HSV";
        public static readonly string IMAGE_DIR = ROOT_DIR + @"\images";
        public static readonly string IMAGE_EXT = ".webp";

        private readonly Page[] _appPages;
        private static int _currPageNum = 0;
        public AppWindow appWindow;

        public static Gallery gallery;
        public static List<Gallery> bmGalleries;

        public MainWindow() {
            InitializeComponent();

            Title = "Hitomi Scroll Viewer";

            // create directories if they don't exist
            Directory.CreateDirectory(ROOT_DIR);
            Directory.CreateDirectory(IMAGE_DIR);

            SearchPage sp = new(this);
            ImageWatchingPage iwp = new(this);
            _appPages = new Page[] { sp, iwp };

            SearchPage.Init(iwp);
            iwp.Init(sp);

            // Switch page on double click
            RootFrame.DoubleTapped += (object _, DoubleTappedRoutedEventArgs _) => {
                iwp.SetAutoScroll(false);
                SwitchPage();
            };

            IntPtr windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WindowId windowId = Win32Interop.GetWindowIdFromWindow(windowHandle);
            appWindow = AppWindow.GetFromWindowId(windowId);

            // Maximise window on load
            RootFrame.Loaded += (object _, RoutedEventArgs _) => {
                ((OverlappedPresenter)appWindow.Presenter).Maximize();
            };

            // Handle window close
            Closed += (object _, WindowEventArgs _) => {
                iwp.SetAutoScroll(false);
                if (gallery != null) {
                    if (!IsBookmarked()) {
                        DeleteGallery(gallery);
                    }
                }
                while (ImageWatchingPage.galleryState == ImageWatchingPage.GalleryState.Bookmarking) {
                    Task.Delay(10);
                }
            };

            RootFrame.Content = _appPages[_currPageNum];
        }

        public void SwitchPage() {
            _currPageNum = (_currPageNum + 1) % _appPages.Length;
            RootFrame.Content = _appPages[_currPageNum];
        }

        public Page GetPage() {
            return _appPages[_currPageNum];
        }

        private readonly ContentDialog _dialog = new() {
            CloseButtonText = "Ok",
        };

        public async void AlertUser(string title, string text) {
            _dialog.Title = title;
            _dialog.Content = text;
            _dialog.XamlRoot = Content.XamlRoot;
            await _dialog.ShowAsync();
        }

        public static bool IsBookmarked() {
            for (int i = 0; i < bmGalleries.Count; i++) {
                if (bmGalleries[i].id == gallery.id) {
                    return true;
                }
            }
            return false;
        }

        public static bool IsBookmarkFull() {
            return bmGalleries.Count == SearchPage.MAX_BOOKMARK_PAGE * SearchPage.MAX_BOOKMARK_PER_PAGE;
        }

        public static async Task SaveGallery(string id, byte[][] imageBytes) {
            string path = IMAGE_DIR + @"\" + id;
            Directory.CreateDirectory(path);

            List<Task> tasks = new();
            for (int i = 0; i < imageBytes.Length; i++) {
                if (imageBytes[i] != null) {
                    tasks.Add(File.WriteAllBytesAsync(path + @"\" + i.ToString() + IMAGE_EXT, imageBytes[i]));
                }
            }
            await Task.WhenAll(tasks);
        }

        public static void DeleteGallery(Gallery removingGallery) {
            string path = IMAGE_DIR + @"\" + removingGallery.id;
            if (Directory.Exists(path)) {
                Directory.Delete(path, true);
            }
        }
    }
}
