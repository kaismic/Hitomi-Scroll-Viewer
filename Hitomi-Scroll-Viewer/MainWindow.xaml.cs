using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Hitomi_Scroll_Viewer {
    public sealed partial class MainWindow : Window {
        public static readonly string ROOT_DIR = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\HSV";
        public static readonly string IMAGE_DIR = ROOT_DIR + @"\images";
        public static readonly string IMAGE_EXT = ".webp";

        private readonly SearchPage _sp;
        private readonly ImageWatchingPage _iwp;
        private readonly Page[] _appPages;
        private static int _currPageNum = 0;

        public AppWindow appWindow;

        public Gallery gallery;
        public List<Gallery> bmGalleries;

        public CancellationTokenSource cts;
        public readonly object actionLock = new();
        public bool isInAction = false;

        public MainWindow() {
            InitializeComponent();

            // create directories if they don't exist
            Directory.CreateDirectory(ROOT_DIR);
            Directory.CreateDirectory(IMAGE_DIR);

            _sp = new(this);
            _iwp = new(this);
            _appPages = new Page[] { _sp, _iwp };

            _sp.Init(_iwp);

            // Switch page on double click
            RootFrame.DoubleTapped += (object _, DoubleTappedRoutedEventArgs _) => {
                _iwp.StartStopAutoScroll(false);
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
                HandleClose();
                if (gallery != null) {
                    if (!IsBookmarked()) {
                        DeleteGallery(gallery);
                    }
                }
            };

            RootFrame.Content = _appPages[_currPageNum];
        }

        public void SwitchPage() {
            _currPageNum = (_currPageNum + 1) % _appPages.Length;
            RootFrame.Content = _appPages[_currPageNum];
        }

        private readonly ContentDialog _dialog = new() {
            CloseButtonText = "Ok",
            Title = new TextBlock() {
                TextWrapping = TextWrapping.WrapWholeWords
            },
            Content = new TextBlock() {
                TextWrapping = TextWrapping.WrapWholeWords
            }
        };

        public async void AlertUser(string title, string text) {
            ((TextBlock)_dialog.Title).Text = title;
            ((TextBlock)_dialog.Content).Text = text;
            _dialog.XamlRoot = Content.XamlRoot;
            await _dialog.ShowAsync();
        }

        public bool IsBookmarked() {
            for (int i = 0; i < bmGalleries.Count; i++) {
                if (bmGalleries[i].id == gallery.id) {
                    return true;
                }
            }
            return false;
        }

        public bool IsBookmarkFull() {
            return bmGalleries.Count == SearchPage.MAX_BOOKMARK_PAGE * SearchPage.MAX_BOOKMARK_PER_PAGE;
        }

        public static void DeleteGallery(Gallery removingGallery) {
            string path = IMAGE_DIR + @"\" + removingGallery.id;
            if (Directory.Exists(path)) Directory.Delete(path, true);
        }

        public void StartAction(bool start) {
            lock (actionLock) {
                if (start) {
                    isInAction = true;
                    _iwp.LoadingControlBtn.Label = "Cancel Loading";
                    _iwp.LoadingControlBtn.Icon = new SymbolIcon(Symbol.Cancel);
                }
                _sp.EnableControls(!start);
                _iwp.EnableControls(!start);
                if (!start) {
                    _iwp.LoadingControlBtn.Label = "Reload Gallery";
                    _iwp.LoadingControlBtn.Icon = new SymbolIcon(Symbol.Sync);
                    isInAction = false;
                }
            }
        }

        private async void HandleClose() {
            _iwp.WaitBookmarking();
            lock (actionLock) if (isInAction) cts.Cancel();
            while (isInAction) {
                await Task.Delay(10);
            }
            cts.Dispose();
        }
    }
}
