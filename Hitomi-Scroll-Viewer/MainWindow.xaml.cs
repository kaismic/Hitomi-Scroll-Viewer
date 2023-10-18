using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using static Hitomi_Scroll_Viewer.Utils;

namespace Hitomi_Scroll_Viewer {
    public sealed partial class MainWindow : Window {
        public readonly SearchPage sp;
        private readonly ImageWatchingPage _iwp;
        private readonly Page[] _appPages;
        private static int _currPageNum = 0;

        public Gallery gallery;
        public List<Gallery> bmGalleries;
        public readonly Mutex bmMutex = new();

        public readonly HttpClient httpClient = new() {
            DefaultRequestHeaders = {
                {"referer", REFERER }
            }
        };

        public MainWindow() {
            InitializeComponent();

            // create directories if they don't exist
            Directory.CreateDirectory(ROOT_DIR);
            Directory.CreateDirectory(IMAGE_DIR);

            sp = new(this);
            _iwp = new(this);
            SearchPage.Init(_iwp);
            _appPages = new Page[] { sp, _iwp };

            // Switch page on double click
            RootFrame.DoubleTapped += (_, _) => {
                SwitchPage();
            };

            // Maximise window on load
            RootFrame.Loaded += (_, _) => {
                ((OverlappedPresenter)AppWindow.Presenter).Maximize();
            };

            // Handle window closing
            AppWindow.Closing += (AppWindow _, AppWindowClosingEventArgs e) => {
                if (sp.IsBusy()) {
                    e.Cancel = true;
                    return;
                }
                if (_iwp.IsBusy()) {
                    e.Cancel = true;
                    return;
                }
                if (gallery != null) {
                    if (GetGalleryFromBookmark(gallery.id) == null) {
                        DeleteGallery(gallery);
                    }
                }
                File.WriteAllText(SETTINGS_PATH, JsonSerializer.Serialize(_iwp.GetSettings(), serializerOptions));
            };

            RootFrame.KeyDown += (object _, KeyRoutedEventArgs e) => {
                if (_currPageNum == 1) _iwp.HandleKeyDown(_, e);
            };

            RootFrame.Content = _appPages[_currPageNum];
        }

        public void SwitchPage() {
            if (_currPageNum == 1 && _iwp.IsAutoScrolling) _iwp.StartStopAutoScroll(false);
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

        /**
         * <returns>The <c>Gallery</c> if the gallery with the given id is bookmarked, otherwise <c>null</c>.</returns>
         */
        public Gallery GetGalleryFromBookmark(string id) {
            for (int i = 0; i < bmGalleries.Count; i++) {
                if (bmGalleries[i].id == id) {
                    return bmGalleries[i];
                }
            }
            return null;
        }
    }
}
