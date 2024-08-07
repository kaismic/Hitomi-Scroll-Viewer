﻿using HitomiScrollViewerLib.Controls.SearchPageComponents;
using HitomiScrollViewerLib.Entities;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.ApplicationModel.Resources;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using static HitomiScrollViewerLib.SharedResources;
using static HitomiScrollViewerLib.Utils;

namespace HitomiScrollViewerLib.Controls.Pages
{
    public sealed partial class SearchPage : Page {
        private static readonly ResourceMap _resourceMap = MainResourceMap.GetSubtree("SearchPage");

        private static readonly Range GALLERY_ID_LENGTH_RANGE = 6..7;

        private readonly IEnumerable<int> _bookmarkNumPerPageRange = Enumerable.Range(1, 8);
        internal static readonly List<BookmarkItem> BookmarkItems = [];
        private static readonly Dictionary<string, BookmarkItem> BookmarkDict = [];
        private static readonly object _bmLock = new();

        private static readonly DataPackage _myDataPackage = new() {
            RequestedOperation = DataPackageOperation.Copy
        };

        private readonly ObservableCollection<SearchLinkItem> _searchLinkItems = [];

        internal readonly ObservableCollection<DownloadItem> DownloadingItems = [];
        internal static readonly ConcurrentDictionary<int, byte> DownloadingGalleryIds = [];

        public SearchPage() {
            InitializeComponent();

            //if (File.Exists(TAG_FILTERS_FILE_PATH)) {
            //    TagFilterDict = (Dictionary<string, TagFilter>)JsonSerializer.Deserialize(
            //        File.ReadAllText(TAG_FILTERS_FILE_PATH),
            //        typeof(Dictionary<string, TagFilter>),
            //        DEFAULT_SERIALIZER_OPTIONS
            //    );
            //} else {
            //    TagFilterDict = new() {
            //        { EXAMPLE_TAG_FILTER_NAME_1, new() },
            //        { EXAMPLE_TAG_FILTER_NAME_2, new() },
            //        { EXAMPLE_TAG_FILTER_NAME_3, new() },
            //        { EXAMPLE_TAG_FILTER_NAME_4, new() },
            //    };
            //    TagFilterDict[EXAMPLE_TAG_FILTER_NAME_1].IncludeTagFilters["language"].Add("english");
            //    TagFilterDict[EXAMPLE_TAG_FILTER_NAME_1].IncludeTagFilters["tag"].Add("full_color");
            //    TagFilterDict[EXAMPLE_TAG_FILTER_NAME_1].ExcludeTagFilters["type"].Add("gamecg");

            //    TagFilterDict[EXAMPLE_TAG_FILTER_NAME_2].IncludeTagFilters["type"].Add("doujinshi");
            //    TagFilterDict[EXAMPLE_TAG_FILTER_NAME_2].IncludeTagFilters["series"].Add("naruto");
            //    TagFilterDict[EXAMPLE_TAG_FILTER_NAME_2].IncludeTagFilters["language"].Add("korean");

            //    TagFilterDict[EXAMPLE_TAG_FILTER_NAME_3].IncludeTagFilters["series"].Add("blue_archive");
            //    TagFilterDict[EXAMPLE_TAG_FILTER_NAME_3].IncludeTagFilters["female"].Add("sole_female");
            //    TagFilterDict[EXAMPLE_TAG_FILTER_NAME_3].ExcludeTagFilters["language"].Add("chinese");

            //    TagFilterDict[EXAMPLE_TAG_FILTER_NAME_4].ExcludeTagFilters["tag"].Add("non-h_imageset");

            //    WriteTagFilterDict();
            //}
            DownloadInputTextBox.TextChanged += (_, _) => { DownloadButton.IsEnabled = DownloadInputTextBox.Text.Length != 0; };

            Loaded += SearchPage_Loaded;
        }

        private void SearchPage_Loaded(object _0, RoutedEventArgs _1) {
            Loaded -= SearchPage_Loaded;
            PopupInfoBarStackPanel.Margin = new Thickness(0, 0, 0, ActualHeight / 16);

            // TODO
            // if app upgraded from v2 -> v3:
            // 1. Migrate tag filter set
            // 2. Migrate galleries (bookmarks)
            // 3. Migrate images from roaming to local folder

            //bool v2TagFilterExists = File.Exists(TAG_FILTERS_FILE_PATH_V2);
            //bool v2BookmarksExists = File.Exists(BOOKMARKS_FILE_PATH_V2);
            //if (v2TagFilterExists || v2BookmarksExists) {
            //    MigrationProgressReporter reporter = new();
            //    if (v2TagFilterExists) {
            //        Dictionary<string, TagFilterSet> tagFilterSetDict = (Dictionary<string, TagFilter>)JsonSerializer.Deserialize(
            //            File.ReadAllText(TAG_FILTERS_FILE_PATH_V2),
            //            typeof(Dictionary<string, TagFilter>),
            //            DEFAULT_SERIALIZER_OPTIONS
            //        );
            //    }
            //}
            TagFilterSetEditor.Init();
        }

        private void HyperlinkCreateButton_Clicked(object _0, RoutedEventArgs _1) {
            SearchLinkItem searchLinkItem = TagFilterSetEditor.GetSearchLinkItem(_searchLinkItems);
            if (searchLinkItem != null) {
                _searchLinkItems.Add(searchLinkItem);
                // copy link to clipboard
                _myDataPackage.SetText(searchLinkItem.SearchLink);
            }
            Clipboard.SetContent(_myDataPackage);
        }

        private void DownloadBtn_Clicked(object _0, RoutedEventArgs _1) {
            string idPattern = @"\d{" + GALLERY_ID_LENGTH_RANGE.Start + "," + GALLERY_ID_LENGTH_RANGE.End + "}";
            string[] urlOrIds = DownloadInputTextBox.Text.Split(NEW_LINE_SEPS, DEFAULT_STR_SPLIT_OPTIONS);
            if (urlOrIds.Length == 0) {
                MainWindow.CurrentMainWindow.NotifyUser(_resourceMap.GetValue("Notification_DownloadInputTextBox_Empty_Title").ValueAsString, "");
                return;
            }
            List<int> extractedIds = [];
            foreach (string urlOrId in urlOrIds) {
                MatchCollection matches = Regex.Matches(urlOrId, idPattern);
                if (matches.Count > 0) {
                    extractedIds.Add(int.Parse(matches.Last().Value));
                }
            }
            if (extractedIds.Count == 0) {
                MainWindow.CurrentMainWindow.NotifyUser(
                    _resourceMap.GetValue("Notification_DownloadInputTextBox_Invalid_Title").ValueAsString,
                    ""
                );
                return;
            }
            DownloadInputTextBox.Text = "";
            
            // only download if the gallery is not already downloading
            foreach (int id in extractedIds) {
                TryDownload(id);
            }
        }

        internal bool TryDownload(int id, BookmarkItem bookmarkItem = null) {
            if (DownloadingGalleryIds.TryAdd(id, 0)) {
                DownloadingItems.Add(new(id, bookmarkItem));
                return true;
            }
            return false;
        }

        private const int INFOBAR_DISPLAY_DURATION = 5000;
        internal void ShowInfoBar(string message) {
            InfoBar infoBar = new() {
                Message = message,
                IsOpen = true
            };
            CancellationTokenSource cts = new();
            infoBar.CloseButtonClick += (InfoBar infoBar, object _) => {
                cts.Cancel();
            };
            PopupInfoBarStackPanel.Children.Add(infoBar);
            Task.Run(
                async () => {
                    await Task.Delay(INFOBAR_DISPLAY_DURATION, cts.Token);
                    DispatcherQueue.TryEnqueue(() => PopupInfoBarStackPanel.Children.Remove(infoBar));
                },
                cts.Token
            );
        }

        private DateTime _lastWindowSizeChangeTime;
        internal async void Window_SizeChanged(WindowSizeChangedEventArgs e) {
            DateTime thisDateTime = _lastWindowSizeChangeTime = DateTime.UtcNow;
            // wait for a short time to check if there is a later SizeChanged event to prevent unnecessary rapid method calls
            await Task.Delay(200);
            if (_lastWindowSizeChangeTime != thisDateTime) {
                return;
            }
            MainControlGrid.Height = e.Size.Height - MainContainerStackPanel.Padding.Top - MainContainerStackPanel.Padding.Bottom;
        }

        //public BookmarkItem AddBookmark(Gallery gallery) {
        //    lock (_bmLock) {
        //        // return the BookmarkItem if it is already bookmarked
        //        if (BookmarkDict.TryGetValue(gallery.id, out BookmarkItem bmItem)) {
        //            return bmItem;
        //        }
        //        bmItem = new BookmarkItem(gallery, true);
        //        BookmarkItems.Add(bmItem);
        //        BookmarkDict.Add(gallery.id, bmItem);
        //        // new page is needed
        //        if (BookmarkItems.Count % (BookmarkNumPerPageSelector.SelectedIndex + 1) == 1) {
        //            BookmarkPageSelector.Items.Add(BookmarkPageSelector.Items.Count + 1);
        //        }
        //        WriteObjectToJson(BOOKMARKS_FILE_PATH, BookmarkItems.Select(bmItem => bmItem.gallery));
        //        if (BookmarkItems.Count == 1) {
        //            BookmarkPageSelector.SelectedIndex = 0;
        //        } else {
        //            UpdateBookmarkLayout();
        //        }
        //        return bmItem;
        //    }
        //}

        //public void RemoveBookmark(BookmarkItem bmItem) {
        //    lock (_bmLock) {
        //        string path = Path.Combine(IMAGE_DIR, bmItem.gallery.id);
        //        if (Directory.Exists(path)) Directory.Delete(path, true);
        //        BookmarkItems.Remove(bmItem);
        //        BookmarkDict.Remove(bmItem.gallery.id);
        //        WriteObjectToJson(BOOKMARKS_FILE_PATH, BookmarkItems.Select(bmItem => bmItem.gallery));

        //        bool pageChanged = false;
        //        // a page needs to be removed
        //        if (BookmarkItems.Count % (BookmarkNumPerPageSelector.SelectedIndex + 1) == 0) {
        //            // if current page is the last page
        //            if (BookmarkPageSelector.SelectedIndex == BookmarkPageSelector.Items.Count - 1) {
        //                pageChanged = true;
        //                BookmarkPageSelector.SelectedIndex = 0;
        //            }
        //            BookmarkPageSelector.Items.RemoveAt(BookmarkPageSelector.Items.Count - 1);
        //        }
        //        // don't call UpdateBookmarkLayout again if BookmarkPageSelector.SelectedIndex is set to 0 because UpdateBookmarkLayout is already called by SelectionChanged event
        //        if (!pageChanged) {
        //            UpdateBookmarkLayout();
        //        }
        //    }
        //}

        //internal void SwapBookmarks(BookmarkItem bmItem, BookmarkSwapDirection dir) {
        //    lock (_bmLock) {
        //        int idx = BookmarkItems.FindIndex(item => item.gallery.id == bmItem.gallery.id);
        //        switch (dir) {
        //            case BookmarkSwapDirection.Up: {
        //                if (idx == 0) {
        //                    return;
        //                }
        //                (BookmarkItems[idx], BookmarkItems[idx - 1]) = (BookmarkItems[idx - 1], BookmarkItems[idx]);
        //                break;
        //            }
        //            case BookmarkSwapDirection.Down: {
        //                if (idx == BookmarkItems.Count - 1) {
        //                    return;
        //                }
        //                (BookmarkItems[idx], BookmarkItems[idx + 1]) = (BookmarkItems[idx + 1], BookmarkItems[idx]);
        //                break;
        //            }
        //        }
        //        WriteObjectToJson(BOOKMARKS_FILE_PATH, BookmarkItems.Select(bmItem => bmItem.gallery));
        //        UpdateBookmarkLayout();
        //    }
        //}

        //private void UpdateBookmarkLayout() {
        //    BookmarkPanel.Children.Clear();
        //    int page = BookmarkPageSelector.SelectedIndex;
        //    if (page < 0) {
        //        return;
        //    }
        //    int bookmarkNumPerPage = BookmarkNumPerPageSelector.SelectedIndex + 1;
        //    for (int i = page * bookmarkNumPerPage; i < Math.Min((page + 1) * bookmarkNumPerPage, BookmarkItems.Count); i++) {
        //        BookmarkPanel.Children.Add(BookmarkItems[i]);
        //    }
        //}

        //private void BookmarkNumPerPageSelector_SelectionChanged(object _0, SelectionChangedEventArgs arg) {
        //    if (arg.AddedItems.Count == 0 || BookmarkItems.Count == 0) {
        //        return;
        //    }
        //    BookmarkPageSelector.Items.Clear();
        //    int numOfPages = (int)Math.Ceiling((double)BookmarkItems.Count / (BookmarkNumPerPageSelector.SelectedIndex + 1));
        //    for (int i = 0; i < numOfPages; i++) {
        //        BookmarkPageSelector.Items.Add(i + 1);
        //    }
        //    BookmarkPageSelector.SelectedIndex = 0;
        //}
    }
}
