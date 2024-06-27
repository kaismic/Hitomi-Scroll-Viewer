using Hitomi_Scroll_Viewer.MainWindowComponent.SearchPageComponent;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.Windows.ApplicationModel.Resources;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using static Hitomi_Scroll_Viewer.Resources;
using static Hitomi_Scroll_Viewer.TagFilter;
using static Hitomi_Scroll_Viewer.Utils;

namespace Hitomi_Scroll_Viewer.MainWindowComponent {
    public sealed partial class SearchPage : Page {
        private static readonly ResourceMap ResourceMap = MainResourceMap.GetSubtree("SearchPage");

        private static readonly string BOOKMARKS_FILE_PATH = Path.Combine(ROOT_DIR, "bookmarks.json");
        private static readonly string TAG_FILTERS_FILE_PATH = Path.Combine(ROOT_DIR, "tag_filters.json");

        private static readonly string SEARCH_ADDRESS = "https://hitomi.la/search.html?";
        private static readonly Range GALLERY_ID_LENGTH_RANGE = 6..7;

        private readonly Dictionary<string, TagFilter> _tagFilterDict;
        private readonly ObservableCollection<string> _tagFilterDictKeys;
        private readonly ObservableCollection<SearchFilterItem> _searchFilterItems;

        private static readonly int MAX_BOOKMARK_PER_PAGE = 3;

        public static readonly List<BookmarkItem> BookmarkItems = [];
        private static readonly object _bmLock = new();

        public enum BookmarkSwapDirection {
            Up, Down
        }

        private static readonly DataPackage _myDataPackage = new() {
            RequestedOperation = DataPackageOperation.Copy
        };

        private readonly ObservableCollection<SearchLinkItem> _searchLinkItems = [];

        public readonly ObservableCollection<DownloadItem> DownloadingItems = [];
        public static readonly ConcurrentDictionary<string, byte> DownloadingGalleries = [];

        private string _currTagFilterName;

        private readonly ContentDialog _confirmDialog = new() {
            IsPrimaryButtonEnabled = true,
            PrimaryButtonText = DIALOG_BUTTON_TEXT_YES,
            CloseButtonText = DIALOG_BUTTON_TEXT_CANCEL,
            Title = new TextBlock() {
                TextWrapping = TextWrapping.WrapWholeWords
            },
            Content = new TextBlock() {
                TextWrapping = TextWrapping.WrapWholeWords
            }
        };

        public SearchPage() {
            InitializeComponent();
            InitLayout();

            if (File.Exists(TAG_FILTERS_FILE_PATH)) {
                _tagFilterDict = (Dictionary<string, TagFilter>)JsonSerializer.Deserialize(
                    File.ReadAllText(TAG_FILTERS_FILE_PATH),
                    typeof(Dictionary<string, TagFilter>),
                    DEFAULT_SERIALIZER_OPTIONS
                );
            } else {
                _tagFilterDict = new() {
                    { EXAMPLE_TAG_FILTER_NAME_1, new() },
                    { EXAMPLE_TAG_FILTER_NAME_2, new() },
                    { EXAMPLE_TAG_FILTER_NAME_3, new() },
                };
                _tagFilterDict[EXAMPLE_TAG_FILTER_NAME_1].includeTags["language"].Add("english");
                _tagFilterDict[EXAMPLE_TAG_FILTER_NAME_1].includeTags["tag"].Add("full_color");
                _tagFilterDict[EXAMPLE_TAG_FILTER_NAME_1].excludeTags["type"].Add("gamecg");

                _tagFilterDict[EXAMPLE_TAG_FILTER_NAME_2].includeTags["type"].Add("doujinshi");
                _tagFilterDict[EXAMPLE_TAG_FILTER_NAME_2].includeTags["type"].Add("manga");
                _tagFilterDict[EXAMPLE_TAG_FILTER_NAME_2].includeTags["series"].Add("naruto");
                _tagFilterDict[EXAMPLE_TAG_FILTER_NAME_2].includeTags["language"].Add("korean");

                _tagFilterDict[EXAMPLE_TAG_FILTER_NAME_3].includeTags["series"].Add("blue_archive");
                _tagFilterDict[EXAMPLE_TAG_FILTER_NAME_3].includeTags["female"].Add("sole_female");
                _tagFilterDict[EXAMPLE_TAG_FILTER_NAME_3].excludeTags["language"].Add("chinese");
                WriteTagFilters();
            }

            _tagFilterDictKeys = new(_tagFilterDict.Keys);
            _searchFilterItems = new(_tagFilterDict.Keys.Select(key => new SearchFilterItem(key, TagNameCheckBox_StateChanged)));

            // create bookmarked galleries' info file if it doesn't exist
            if (!File.Exists(BOOKMARKS_FILE_PATH)) {
                WriteObjectToJson(BOOKMARKS_FILE_PATH, new List<Gallery>());
            }
            // read bookmarked galleries' info from file
            List<Gallery> galleries = (List<Gallery>)JsonSerializer.Deserialize(
                File.ReadAllText(BOOKMARKS_FILE_PATH),
                typeof(List<Gallery>),
                DEFAULT_SERIALIZER_OPTIONS
                );

            int pages = galleries.Count / MAX_BOOKMARK_PER_PAGE + (galleries.Count % MAX_BOOKMARK_PER_PAGE > 0 ? 1 : 0);
            for (int i = 0; i < pages; i++) {
                BookmarkPageSelector.Items.Add(i);
            }
            if (pages > 0) {
                BookmarkPageSelector.SelectedIndex = 0;
            }
            BookmarkPageSelector.SelectionChanged += (_, _) => UpdateBookmark();

            // fill bookmarks
            foreach (Gallery gallery in galleries) {
                BookmarkItems.Add(new(gallery, false));
            }
            UpdateBookmark();
        }

        private void InitLayout() {
            // Create Tag Control Buttons
            Button[] controlButtons = new Button[5];
            CornerRadius radius = new(4);
            string[] controlButtonTexts = [
                ResourceMap.GetValue("ButtonText_CreateTagFilter").ValueAsString,
                ResourceMap.GetValue("ButtonText_RenameTagFilter").ValueAsString,
                ResourceMap.GetValue("ButtonText_SaveTagFilter").ValueAsString,
                ResourceMap.GetValue("ButtonText_DeleteTagFilter").ValueAsString,
                ResourceMap.GetValue("ButtonText_ClearTextBoxes").ValueAsString
            ];
            SolidColorBrush[] controlButtonBorderColors = [
                new(Colors.Blue),
                new(Colors.Orange),
                new(Colors.Green),
                new(Colors.Red),
                new(Colors.Black)
            ];
            RoutedEventHandler[] controlButtonEventHandlers = [
                CreateTagFilterBtn_Clicked,
                RenameTagFilterBtn_Clicked,
                SaveTagFilterBtn_Clicked,
                DeleteTagFilterBtn_Clicked,
                ClearTextBoxesBtn_Clicked
            ];
            for (int i = 0; i < controlButtons.Length; i++) {
                controlButtons[i] = new() {
                    CornerRadius = radius,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    BorderBrush = controlButtonBorderColors[i],
                    Content = new TextBlock() {
                        Text = controlButtonTexts[i],
                        TextWrapping = TextWrapping.WrapWholeWords
                    }
                };
                controlButtons[i].Click += controlButtonEventHandlers[i];
                Grid.SetRow(controlButtons[i], i);
                ControlButtonContainer.RowDefinitions.Add(new());
                ControlButtonContainer.Children.Add(controlButtons[i]);
            }

            void SetConfirmDialogXamlRoot(object _0, RoutedEventArgs _1) {
                _confirmDialog.XamlRoot = XamlRoot;
                Loaded -= SetConfirmDialogXamlRoot;
            };
            Loaded += SetConfirmDialogXamlRoot;
        }

        private async Task<ContentDialogResult> ShowConfirmDialogAsync(string title, string content) {
            (_confirmDialog.Title as TextBlock).Text = title;
            (_confirmDialog.Content as TextBlock).Text = content;
            ContentDialogResult result = await _confirmDialog.ShowAsync();
            return result;
        }

        private void TagNameCheckBox_StateChanged(object sender, RoutedEventArgs e) {
            CreateHyperlinkBtn.IsEnabled = _searchFilterItems.Any(item => (bool)item.IsChecked);
        }

        private void AddToTagsDict(string tagName, TagFilter tagFilter) {
            _tagFilterDict.Add(tagName, tagFilter);
            _tagFilterDictKeys.Add(tagName);
            _searchFilterItems.Add(new(tagName, TagNameCheckBox_StateChanged));
        }
        
        private void RemoveFromTagsDict(string tagName) {
            _tagFilterDict.Remove(tagName);
            _tagFilterDictKeys.Remove(tagName);
            _searchFilterItems.Remove(_searchFilterItems.First(item => item.TagName == tagName));
            CreateHyperlinkBtn.IsEnabled = _searchFilterItems.Count != 0;
        }

        private static readonly string NOTIFICATION_TAG_FILTER_NAME_EMPTY_TITLE = ResourceMap.GetValue("Notification_TagFilter_NameEmpty_Title").ValueAsString;
        private static readonly string NOTIFICATION_TAG_FILTER_NAME_EMPTY_CONTENT = ResourceMap.GetValue("Notification_TagFilter_NameEmpty_Content").ValueAsString;
        private static readonly string NOTIFICATION_TAG_FILTER_NAME_DUP_TITLE = ResourceMap.GetValue("Notification_TagFilter_NameDup_Title").ValueAsString;
        private static readonly string NOTIFICATION_TAG_FILTER_NAME_DUP_CONTENT = ResourceMap.GetValue("Notification_TagFilter_NameDup_Content").ValueAsString;
        private static readonly string NOTIFICATION_TAG_FILTER_OVERLAP_TITLE = ResourceMap.GetValue("Notification_TagFilter_Overlap_Title").ValueAsString;
        
        private static readonly string NOTIFICATION_TAG_FILTER_CREATE_1_TITLE = ResourceMap.GetValue("Notification_TagFilter_Create_1_Title").ValueAsString;
        private static readonly string NOTIFICATION_TAG_FILTER_CREATE_2_TITLE = ResourceMap.GetValue("Notification_TagFilter_Create_2_Title").ValueAsString;

        private async void CreateTagFilterBtn_Clicked(object _0, RoutedEventArgs _1) {
            string newTagFilterName = TagNameTextBox.Text.Trim();
            if (newTagFilterName.Length == 0) {
                MainWindow.NotifyUser(NOTIFICATION_TAG_FILTER_NAME_EMPTY_TITLE, NOTIFICATION_TAG_FILTER_NAME_EMPTY_CONTENT);
                return;
            }
            if (_tagFilterDict.ContainsKey(newTagFilterName)) {
                MainWindow.NotifyUser(NOTIFICATION_TAG_FILTER_NAME_DUP_TITLE, NOTIFICATION_TAG_FILTER_NAME_DUP_CONTENT);
                return;
            }
            string overlappingTagFiltersText = GetCurrTagFilter().GetIncludeExcludeOverlap();
            if (overlappingTagFiltersText != "") {
                MainWindow.NotifyUser(NOTIFICATION_TAG_FILTER_OVERLAP_TITLE, overlappingTagFiltersText);
                return;
            }
            ContentDialogResult cdr = await ShowConfirmDialogAsync(string.Format(NOTIFICATION_TAG_FILTER_CREATE_1_TITLE, newTagFilterName), "");
            if (cdr != ContentDialogResult.Primary) {
                return;
            }
            TagNameTextBox.Text = "";
            AddToTagsDict(newTagFilterName, GetCurrTagFilter());
            FilterTagComboBox.SelectedItem = newTagFilterName;
            WriteTagFilters();
            MainWindow.NotifyUser(string.Format(NOTIFICATION_TAG_FILTER_CREATE_2_TITLE, newTagFilterName), "");
        }

        private static readonly string NOTIFICATION_TAG_FILTER_RENAME_1_TITLE = ResourceMap.GetValue("Notification_TagFilter_Rename_1_Title").ValueAsString;
        private static readonly string NOTIFICATION_TAG_FILTER_RENAME_2_TITLE = ResourceMap.GetValue("Notification_TagFilter_Rename_2_Title").ValueAsString;

        private async void RenameTagFilterBtn_Clicked(object _0, RoutedEventArgs _1) {
            if (_currTagFilterName == null) return;
            string oldTagFilterName = _currTagFilterName;
            string newTagFilterName = TagNameTextBox.Text.Trim();
            if (newTagFilterName.Length == 0) {
                MainWindow.NotifyUser(NOTIFICATION_TAG_FILTER_NAME_EMPTY_TITLE, NOTIFICATION_TAG_FILTER_NAME_EMPTY_CONTENT);
                return;
            }
            if (_tagFilterDict.ContainsKey(newTagFilterName)) {
                MainWindow.NotifyUser(NOTIFICATION_TAG_FILTER_NAME_DUP_TITLE, NOTIFICATION_TAG_FILTER_NAME_DUP_CONTENT);
                return;
            }
            ContentDialogResult cdr = await ShowConfirmDialogAsync(string.Format(NOTIFICATION_TAG_FILTER_RENAME_1_TITLE, oldTagFilterName, newTagFilterName), "");
            if (cdr != ContentDialogResult.Primary) {
                return;
            }
            TagNameTextBox.Text = "";
            AddToTagsDict(newTagFilterName, _tagFilterDict[oldTagFilterName]);
            FilterTagComboBox.SelectedItem = newTagFilterName;
            RemoveFromTagsDict(oldTagFilterName);
            WriteTagFilters();
            MainWindow.NotifyUser(string.Format(NOTIFICATION_TAG_FILTER_RENAME_2_TITLE, oldTagFilterName, newTagFilterName), "");
        }

        private static readonly string NOTIFICATION_TAG_FILTER_SAVE_1_TITLE = ResourceMap.GetValue("Notification_TagFilter_Save_1_Title").ValueAsString;
        private static readonly string NOTIFICATION_TAG_FILTER_SAVE_1_CONTENT = ResourceMap.GetValue("Notification_TagFilter_Save_1_Content").ValueAsString;
        private static readonly string NOTIFICATION_TAG_FILTER_SAVE_2_TITLE = ResourceMap.GetValue("Notification_TagFilter_Save_2_Title").ValueAsString;

        private async void SaveTagFilterBtn_Clicked(object _0, RoutedEventArgs _1) {
            if (_currTagFilterName == null) return;
            string overlappingTagFiltersText = GetCurrTagFilter().GetIncludeExcludeOverlap();
            if (overlappingTagFiltersText != "") {
                MainWindow.NotifyUser(NOTIFICATION_TAG_FILTER_OVERLAP_TITLE, overlappingTagFiltersText);
                return;
            }
            ContentDialogResult cdr = await ShowConfirmDialogAsync(
                string.Format(NOTIFICATION_TAG_FILTER_SAVE_1_TITLE, _currTagFilterName),
                string.Format(NOTIFICATION_TAG_FILTER_SAVE_1_CONTENT, _currTagFilterName)
            );
            if (cdr != ContentDialogResult.Primary) {
                return;
            }
            _tagFilterDict[_currTagFilterName] = GetCurrTagFilter();
            WriteTagFilters();
            MainWindow.NotifyUser(string.Format(NOTIFICATION_TAG_FILTER_SAVE_2_TITLE, _currTagFilterName), "");
        }

        private static readonly string NOTIFICATION_TAG_FILTER_DELETE_1_TITLE = ResourceMap.GetValue("Notification_TagFilter_Delete_1_Title").ValueAsString;
        private static readonly string NOTIFICATION_TAG_FILTER_DELETE_2_TITLE = ResourceMap.GetValue("Notification_TagFilter_Delete_2_Title").ValueAsString;

        private async void DeleteTagFilterBtn_Clicked(object _0, RoutedEventArgs _1) {
            if (_currTagFilterName == null) return;
            string tagFilterName = _currTagFilterName;
            ContentDialogResult cdr = await ShowConfirmDialogAsync(string.Format(NOTIFICATION_TAG_FILTER_DELETE_1_TITLE, tagFilterName), "");
            if (cdr != ContentDialogResult.Primary) {
                return;
            }
            RemoveFromTagsDict(tagFilterName);
            WriteTagFilters();
            MainWindow.NotifyUser(string.Format(NOTIFICATION_TAG_FILTER_DELETE_2_TITLE, tagFilterName), "");
        }

        private void ClearTextBoxesBtn_Clicked(object _0, RoutedEventArgs _1) {
            IncludeTagContainer.Clear();
            ExcludeTagContainer.Clear();
        }

        public void WriteTagFilters() {
            WriteObjectToJson(TAG_FILTERS_FILE_PATH, _tagFilterDict);
        }

        private void FilterTagComboBox_SelectionChanged(object _0, SelectionChangedEventArgs _1) {
            _currTagFilterName = (string)FilterTagComboBox.SelectedItem;
            if (_currTagFilterName == null) {
                ClearTextBoxesBtn_Clicked(null, null);
                return;
            }
            TagFilter tag = _tagFilterDict[_currTagFilterName];
            IncludeTagContainer.InsertTags(tag.includeTags);
            ExcludeTagContainer.InsertTags(tag.excludeTags);
        }

        private TagFilter GetCurrTagFilter() {
            return new TagFilter() {
                includeTags = IncludeTagContainer.GetTags(),
                excludeTags = ExcludeTagContainer.GetTags()
            };
        }

        private static readonly string NOTIFICATION_SEARCH_LINK_CONTENT_EMPTY_TITLE = ResourceMap.GetValue("Notification_SeachLink_ContentEmpty_Title").ValueAsString;

        private void CreateHyperlinkBtn_Clicked(object _0, RoutedEventArgs _1) {
            var selectedSearchFilterItems = _searchFilterItems.Where(item => (bool)item.IsChecked);
            TagFilter combinedTagFilter = selectedSearchFilterItems.Aggregate(
                new TagFilter(),
                (result, item) => {
                    TagFilter currTagFilter = _tagFilterDict[item.TagName];
                    foreach (string category in CATEGORIES) {
                        result.includeTags[category] = result.includeTags[category].Union(currTagFilter.includeTags[category]).ToHashSet();
                        result.excludeTags[category] = result.excludeTags[category].Union(currTagFilter.excludeTags[category]).ToHashSet();
                    }
                    return result;
                }
            );

            string overlappingTagFiltersText = combinedTagFilter.GetIncludeExcludeOverlap();
            if (overlappingTagFiltersText != "") {
                MainWindow.NotifyUser(NOTIFICATION_TAG_FILTER_OVERLAP_TITLE, overlappingTagFiltersText);
                return;
            }

            string searchParams = string.Join(
                ' ',
                CATEGORIES.Select(category =>
                    (
                        string.Join(' ', combinedTagFilter.includeTags[category].Select(tag => category + ':' + tag.Replace(' ', '_')))
                            + ' ' +
                        string.Join(' ', combinedTagFilter.excludeTags[category].Select(tag => '-' + category + ':' + tag.Replace(' ', '_')))
                    ).Trim()
                ).Where(searchParam => searchParam != "")
            );
            if (searchParams == "") {
                MainWindow.NotifyUser(NOTIFICATION_SEARCH_LINK_CONTENT_EMPTY_TITLE, "");
                return;
            }
            string searchLink = SEARCH_ADDRESS + searchParams;

            string displayText = string.Join(
                Environment.NewLine,
                CATEGORIES.Select(category => {
                    string displayTextPart =
                        (
                            string.Join(' ', combinedTagFilter.includeTags[category].Select(tag => tag.Replace(' ', '_')))
                            + ' ' +
                            string.Join(' ', combinedTagFilter.excludeTags[category].Select(tag => '-' + tag.Replace(' ', '_')))
                        ).Trim();
                    if (displayTextPart != "") {
                        return char.ToUpper(category[0]) + category[1..] + ": " + displayTextPart;
                    } else {
                        return "";
                    }
                }).Where(displayTagTexts => displayTagTexts != "")
            );
            
            _searchLinkItems.Add(new(
                searchLink,
                displayText,
                (_, arg) => _searchLinkItems.Remove((SearchLinkItem)arg.Parameter)
            ));
            // copy link to clipboard
            _myDataPackage.SetText(searchLink);
            Clipboard.SetContent(_myDataPackage);
        }

        private static readonly string NOTIFICATION_GALLERY_ID_TEXTBOX_CONTENT_EMPTY_TITLE = ResourceMap.GetValue("Notification_GalleryIDTextBox_ContentEmpty_Title").ValueAsString;
        private static readonly string NOTIFICATION_GALLERY_ID_TEXTBOX_CONTENT_INVALID_TITLE = ResourceMap.GetValue("Notification_GalleryIDTextBox_ContentInvalid_Title").ValueAsString;
        private static readonly string NOTIFICATION_GALLERY_ID_TEXTBOX_CONTENT_INVALID_CONTENT = ResourceMap.GetValue("Notification_GalleryIDTextBox_ContentInvalid_Content").ValueAsString;

        private void DownloadBtn_Clicked(object _0, RoutedEventArgs _1) {
            string idPattern = @"\d{" + GALLERY_ID_LENGTH_RANGE.Start + "," + GALLERY_ID_LENGTH_RANGE.End + "}";
            string[] urlOrIds = GalleryIDTextBox.Text.Split(NEW_LINE_SEPS, DEFAULT_STR_SPLIT_OPTIONS);
            if (urlOrIds.Length == 0) {
                MainWindow.NotifyUser(NOTIFICATION_GALLERY_ID_TEXTBOX_CONTENT_EMPTY_TITLE, "");
                return;
            }
            List<string> extractedIds = [];
            foreach (string urlOrId in urlOrIds) {
                MatchCollection matches = Regex.Matches(urlOrId, idPattern);
                if (matches.Count > 0) {
                    extractedIds.Add(matches.Last().Value);
                }
            }
            if (extractedIds.Count == 0) {
                MainWindow.NotifyUser(
                    NOTIFICATION_GALLERY_ID_TEXTBOX_CONTENT_INVALID_TITLE,
                    NOTIFICATION_GALLERY_ID_TEXTBOX_CONTENT_INVALID_CONTENT
                );
                return;
            }
            GalleryIDTextBox.Text = "";
            
            foreach (string extractedId in extractedIds) {
                // skip if it is already downloading
                if (DownloadingGalleries.TryGetValue(extractedId, out _)) {
                    continue;
                }
                // Download
                DownloadingGalleries.TryAdd(extractedId, 0);
                DownloadingItems.Add(new(extractedId));
            }
        }

        public BookmarkItem CreateAndAddBookmark(Gallery gallery) {
            lock (_bmLock) {
                // return the BookmarkItem if it is already bookmarked
                var bmItem = GetBookmarkItem(gallery.id);
                if (bmItem != null) {
                    return bmItem;
                }
                bmItem = new BookmarkItem(gallery, true);
                BookmarkItems.Add(bmItem);
                // new page is needed
                if (BookmarkItems.Count % MAX_BOOKMARK_PER_PAGE == 1) {
                    BookmarkPageSelector.Items.Add(BookmarkPageSelector.Items.Count);
                }
                WriteObjectToJson(BOOKMARKS_FILE_PATH, BookmarkItems.Select(bmItem => bmItem.gallery));
                if (BookmarkItems.Count == 1) {
                    BookmarkPageSelector.SelectedIndex = 0;
                } else {
                    UpdateBookmark();
                }
                return bmItem;
            }
        }

        public void DeleteBookmark(BookmarkItem bmItem) {
            lock (_bmLock) {
                string path = Path.Combine(IMAGE_DIR, bmItem.gallery.id);
                if (Directory.Exists(path)) Directory.Delete(path, true);
                BookmarkItems.Remove(bmItem);
                WriteObjectToJson(BOOKMARKS_FILE_PATH, BookmarkItems.Select(bmItem => bmItem.gallery));

                bool pageChanged = false;
                // a page needs to be deleted
                if (BookmarkItems.Count % MAX_BOOKMARK_PER_PAGE == 0) {
                    // if current page is the last page
                    if (BookmarkPageSelector.SelectedIndex == BookmarkPageSelector.Items.Count - 1) {
                        pageChanged = true;
                        BookmarkPageSelector.SelectedIndex = 0;
                    }
                    BookmarkPageSelector.Items.Remove(BookmarkPageSelector.Items.Count - 1);
                }

                // don't call FillBookmarkGrid again if page was changed because BookmarkPageSelector.SelectionChanged event would have called FillBookmarkGrid already
                if (!pageChanged) {
                    UpdateBookmark();
                }
            }
        }

        public void SwapBookmarks(BookmarkItem bmItem, BookmarkSwapDirection dir) {
            lock (_bmLock) {
                int idx = GetBookmarkIndex(bmItem.gallery.id);
                switch (dir) {
                    case BookmarkSwapDirection.Up: {
                        if (idx == 0) {
                            return;
                        }
                        (BookmarkItems[idx], BookmarkItems[idx - 1]) = (BookmarkItems[idx - 1], BookmarkItems[idx]);
                        break;
                    }
                    case BookmarkSwapDirection.Down: {
                        if (idx == BookmarkItems.Count - 1) {
                            return;
                        }
                        (BookmarkItems[idx], BookmarkItems[idx + 1]) = (BookmarkItems[idx + 1], BookmarkItems[idx]);
                        break;
                    }
                }
                WriteObjectToJson(BOOKMARKS_FILE_PATH, BookmarkItems.Select(bmItem => bmItem.gallery));
                UpdateBookmark();
            }
        }

        private void UpdateBookmark() {
            BookmarkPanel.Children.Clear();
            int page = BookmarkPageSelector.SelectedIndex;
            if (page < 0) {
                return;
            }
            for (int i = page * MAX_BOOKMARK_PER_PAGE; i < (page + 1) * MAX_BOOKMARK_PER_PAGE; i++) {
                if (i < BookmarkItems.Count) {
                    BookmarkPanel.Children.Add(BookmarkItems[i]);
                }
            }
        }

        /**
         * <returns>The <c>Gallery</c> if the gallery with the given id is bookmarked, otherwise <c>null</c>.</returns>
         */
        public static BookmarkItem GetBookmarkItem(string id) {
            for (int i = 0; i < BookmarkItems.Count; i++) {
                if (BookmarkItems[i].gallery.id == id) {
                    return BookmarkItems[i];
                }
            }
            return null;
        }

        /**
         * <returns>The bookmark index of the bookmarked gallery. The given id must be from a bookmarked gallery.</returns>
         */
        public static int GetBookmarkIndex(string id) {
            for (int i = 0; i < BookmarkItems.Count; i++) {
                if (BookmarkItems[i].gallery.id == id) {
                    return i;
                }
            }
            throw new ArgumentException("Id must be from a bookmarked gallery.");
        }
    }
}
