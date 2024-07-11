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

        private static readonly string SEARCH_ADDRESS = "https://hitomi.la/search.html?";
        private static readonly Range GALLERY_ID_LENGTH_RANGE = 6..7;

        private Dictionary<string, TagFilter> _tagFilterDict;
        internal Dictionary<string, TagFilter> TagFilterDict {
            get => _tagFilterDict;
            set {
                _tagFilterDict = value;

                _tagFilterDictKeys.Clear();
                FilterTagComboBox.ItemsSource = null;
                _tagFilterDictKeys = new(value.Keys);
                FilterTagComboBox.ItemsSource = _tagFilterDictKeys;

                _searchFilterItems.Clear();
                SearchFilterItemsRepeater.ItemsSource = null;
                _searchFilterItems = new(value.Keys.Select(key => new SearchFilterItem(key, TagNameCheckBox_StateChanged)));
                SearchFilterItemsRepeater.ItemsSource = _searchFilterItems;
            }
        }
        private ObservableCollection<string> _tagFilterDictKeys = [];
        private ObservableCollection<SearchFilterItem> _searchFilterItems = [];

        private static readonly int MAX_BOOKMARK_PER_PAGE = 3;
        internal static readonly List<BookmarkItem> BookmarkItems = [];
        private static readonly Dictionary<string, BookmarkItem> BookmarkDict = [];
        private static readonly object _bmLock = new();

        internal enum BookmarkSwapDirection {
            Up, Down
        }

        private static readonly DataPackage _myDataPackage = new() {
            RequestedOperation = DataPackageOperation.Copy
        };

        private readonly ObservableCollection<SearchLinkItem> _searchLinkItems = [];

        internal readonly ObservableCollection<DownloadItem> DownloadingItems = [];
        internal static readonly ConcurrentDictionary<string, byte> DownloadingGalleryIds = [];

        private string _currTagFilterName;

        private readonly ContentDialog _confirmDialog = new() {
            DefaultButton = ContentDialogButton.Primary,
            PrimaryButtonText = TEXT_YES,
            CloseButtonText = TEXT_CANCEL,
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
                TagFilterDict = (Dictionary<string, TagFilter>)JsonSerializer.Deserialize(
                    File.ReadAllText(TAG_FILTERS_FILE_PATH),
                    typeof(Dictionary<string, TagFilter>),
                    DEFAULT_SERIALIZER_OPTIONS
                );
            } else {
                TagFilterDict = new() {
                    { EXAMPLE_TAG_FILTER_NAME_1, new() },
                    { EXAMPLE_TAG_FILTER_NAME_2, new() },
                    { EXAMPLE_TAG_FILTER_NAME_3, new() },
                    { EXAMPLE_TAG_FILTER_NAME_4, new() },
                };
                TagFilterDict[EXAMPLE_TAG_FILTER_NAME_1].includeTags["language"].Add("english");
                TagFilterDict[EXAMPLE_TAG_FILTER_NAME_1].includeTags["tag"].Add("full_color");
                TagFilterDict[EXAMPLE_TAG_FILTER_NAME_1].excludeTags["type"].Add("gamecg");

                TagFilterDict[EXAMPLE_TAG_FILTER_NAME_2].includeTags["type"].Add("doujinshi");
                TagFilterDict[EXAMPLE_TAG_FILTER_NAME_2].includeTags["series"].Add("naruto");
                TagFilterDict[EXAMPLE_TAG_FILTER_NAME_2].includeTags["language"].Add("korean");

                TagFilterDict[EXAMPLE_TAG_FILTER_NAME_3].includeTags["series"].Add("blue_archive");
                TagFilterDict[EXAMPLE_TAG_FILTER_NAME_3].includeTags["female"].Add("sole_female");
                TagFilterDict[EXAMPLE_TAG_FILTER_NAME_3].excludeTags["language"].Add("chinese");

                TagFilterDict[EXAMPLE_TAG_FILTER_NAME_4].excludeTags["tag"].Add("non-h_imageset");

                WriteTagFilterDict();
            }

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
            BookmarkPageSelector.SelectionChanged += (_, _) => UpdateBookmarkLayout();

            // fill bookmarks
            for (int i = 0; i < galleries.Count; i++) {
                BookmarkItem bmItem = new(galleries[i], false);
                BookmarkItems.Add(bmItem);
                BookmarkDict.Add(galleries[i].id, bmItem);
            }
            UpdateBookmarkLayout();

            BookmarkHeaderTextBlock.Text = TEXT_BOOKMARKS;
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

        public async Task<ContentDialogResult> ShowConfirmDialogAsync(string title, string content) {
            (_confirmDialog.Title as TextBlock).Text = title;
            (_confirmDialog.Content as TextBlock).Text = content;
            ContentDialogResult result = await _confirmDialog.ShowAsync();
            return result;
        }

        private void TagNameCheckBox_StateChanged(object sender, RoutedEventArgs e) {
            CreateHyperlinkBtn.IsEnabled = _searchFilterItems.Any(item => (bool)item.IsChecked);
        }

        private void AddTagFilterDict(string tagName, TagFilter tagFilter) {
            TagFilterDict.Add(tagName, tagFilter);
            _tagFilterDictKeys.Add(tagName);
            _searchFilterItems.Add(new(tagName, TagNameCheckBox_StateChanged));
        }
        
        private void RemoveTagFilterDict(string tagName) {
            TagFilterDict.Remove(tagName);
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
            if (TagFilterDict.ContainsKey(newTagFilterName)) {
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
            AddTagFilterDict(newTagFilterName, GetCurrTagFilter());
            FilterTagComboBox.SelectedItem = newTagFilterName;
            WriteTagFilterDict();
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
            if (TagFilterDict.ContainsKey(newTagFilterName)) {
                MainWindow.NotifyUser(NOTIFICATION_TAG_FILTER_NAME_DUP_TITLE, NOTIFICATION_TAG_FILTER_NAME_DUP_CONTENT);
                return;
            }
            ContentDialogResult cdr = await ShowConfirmDialogAsync(string.Format(NOTIFICATION_TAG_FILTER_RENAME_1_TITLE, oldTagFilterName, newTagFilterName), "");
            if (cdr != ContentDialogResult.Primary) {
                return;
            }
            TagNameTextBox.Text = "";
            AddTagFilterDict(newTagFilterName, TagFilterDict[oldTagFilterName]);
            FilterTagComboBox.SelectedItem = newTagFilterName;
            RemoveTagFilterDict(oldTagFilterName);
            WriteTagFilterDict();
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
            TagFilterDict[_currTagFilterName] = GetCurrTagFilter();
            WriteTagFilterDict();
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
            RemoveTagFilterDict(tagFilterName);
            WriteTagFilterDict();
            MainWindow.NotifyUser(string.Format(NOTIFICATION_TAG_FILTER_DELETE_2_TITLE, tagFilterName), "");
        }

        private void ClearTextBoxesBtn_Clicked(object _0, RoutedEventArgs _1) {
            IncludeTagContainer.Clear();
            ExcludeTagContainer.Clear();
        }

        public void WriteTagFilterDict() {
            WriteObjectToJson(TAG_FILTERS_FILE_PATH, TagFilterDict);
        }

        private void FilterTagComboBox_SelectionChanged(object _0, SelectionChangedEventArgs _1) {
            _currTagFilterName = (string)FilterTagComboBox.SelectedItem;
            if (_currTagFilterName == null) {
                ClearTextBoxesBtn_Clicked(null, null);
                return;
            }
            TagFilter tagFilter = TagFilterDict[_currTagFilterName];
            IncludeTagContainer.InsertTags(tagFilter.includeTags);
            ExcludeTagContainer.InsertTags(tagFilter.excludeTags);
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
                    TagFilter currTagFilter = TagFilterDict[item.TagName];
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
            
            // only download if the gallery is not already downloading
            foreach (string extractedId in extractedIds) {
                TryDownload(extractedId);
            }
        }

        internal bool TryDownload(string id, BookmarkItem bookmarkItem = null) {
            if (DownloadingGalleryIds.TryAdd(id, 0)) {
                DownloadingItems.Add(new(id, bookmarkItem));
                return true;
            }
            return false;
        }

        public BookmarkItem AddBookmark(Gallery gallery) {
            lock (_bmLock) {
                // return the BookmarkItem if it is already bookmarked
                if (BookmarkDict.TryGetValue(gallery.id, out BookmarkItem bmItem)) {
                    return bmItem;
                }
                bmItem = new BookmarkItem(gallery, true);
                BookmarkItems.Add(bmItem);
                BookmarkDict.Add(gallery.id, bmItem);
                // new page is needed
                if (BookmarkItems.Count % MAX_BOOKMARK_PER_PAGE == 1) {
                    BookmarkPageSelector.Items.Add(BookmarkPageSelector.Items.Count);
                }
                WriteObjectToJson(BOOKMARKS_FILE_PATH, BookmarkItems.Select(bmItem => bmItem.gallery));
                if (BookmarkItems.Count == 1) {
                    BookmarkPageSelector.SelectedIndex = 0;
                } else {
                    UpdateBookmarkLayout();
                }
                return bmItem;
            }
        }

        public void RemoveBookmark(BookmarkItem bmItem) {
            lock (_bmLock) {
                string path = Path.Combine(IMAGE_DIR, bmItem.gallery.id);
                if (Directory.Exists(path)) Directory.Delete(path, true);
                BookmarkItems.Remove(bmItem);
                BookmarkDict.Remove(bmItem.gallery.id);
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
                    UpdateBookmarkLayout();
                }
            }
        }

        internal void SwapBookmarks(BookmarkItem bmItem, BookmarkSwapDirection dir) {
            lock (_bmLock) {
                int idx = BookmarkItems.FindIndex(item => item.gallery.id == bmItem.gallery.id);
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
                UpdateBookmarkLayout();
            }
        }

        private void UpdateBookmarkLayout() {
            BookmarkPanel.Children.Clear();
            int page = BookmarkPageSelector.SelectedIndex;
            if (page < 0) {
                return;
            }
            for (int i = page * MAX_BOOKMARK_PER_PAGE; i < Math.Min((page + 1) * MAX_BOOKMARK_PER_PAGE, BookmarkItems.Count); i++) {
                BookmarkPanel.Children.Add(BookmarkItems[i]);
            }
        }
    }
}
