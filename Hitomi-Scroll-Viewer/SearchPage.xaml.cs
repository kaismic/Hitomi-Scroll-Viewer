using Hitomi_Scroll_Viewer.SearchPageComponent;
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

namespace Hitomi_Scroll_Viewer {
    public sealed partial class SearchPage : Page {
        private static readonly ResourceMap ResourceManager = new ResourceManager().MainResourceMap.GetSubtree("SearchPage");
        private static readonly string BUTTON_TEXT_CREATE_TAG_FILTER = ResourceManager.GetValue("ButtonText_CreateTagFilter").ValueAsString;
        private static readonly string BUTTON_TEXT_RENAME_TAG_FILTER = ResourceManager.GetValue("ButtonText_RenameTagFilter").ValueAsString;
        private static readonly string BUTTON_TEXT_SAVE_TAG_FILTER = ResourceManager.GetValue("ButtonText_SaveTagFilter").ValueAsString;
        private static readonly string BUTTON_TEXT_DELETE_TAG_FILTER = ResourceManager.GetValue("ButtonText_DeleteTagFilter").ValueAsString;
        private static readonly string BUTTON_TEXT_CLEAR_TEXTBOXES = ResourceManager.GetValue("ButtonText_ClearTextBoxes").ValueAsString;

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

        private string _currTagName;

        private enum FilterTagAction {
            Create,
            Rename,
            Save,
            Delete,
            Clear
        }

        private readonly ContentDialog _confirmDialog = new() {
            IsPrimaryButtonEnabled = true,
            PrimaryButtonText = DIALOG_TEXT_YES,
            CloseButtonText = DIALOG_TEXT_CANCEL,
            Title = new TextBlock() {
                TextWrapping = TextWrapping.WrapWholeWords
            },
            Content = new TextBlock() {
                TextWrapping = TextWrapping.WrapWholeWords
            }
        };
        private readonly Button[] _controlButtons = new Button[Enum.GetNames<FilterTagAction>().Length];
        private readonly string[] _controlButtonTexts = [
            BUTTON_TEXT_CREATE_TAG_FILTER,
            BUTTON_TEXT_RENAME_TAG_FILTER,
            BUTTON_TEXT_SAVE_TAG_FILTER,
            BUTTON_TEXT_DELETE_TAG_FILTER,
            BUTTON_TEXT_CLEAR_TEXTBOXES
        ];
        private readonly SolidColorBrush[] _controlButtonBorderColors = [
            new(Colors.Blue),
            new(Colors.Orange),
            new(Colors.Green),
            new(Colors.Red),
            new(Colors.Black)
        ];

        public SearchPage() {
            InitializeComponent();
            InitLayout();

            _tagFilterDict = File.Exists(TAG_FILTERS_FILE_PATH)
                ? (Dictionary<string, TagFilter>)JsonSerializer.Deserialize(
                    File.ReadAllText(TAG_FILTERS_FILE_PATH),
                    typeof(Dictionary<string, TagFilter>),
                    serializerOptions
                )
                : new() {
                    { "Tag1", new() },
                    { "Tag2", new() },
                    { "Tag3", new() },
                };

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
                serializerOptions
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
            CornerRadius radius = new(4);
            for (int i = 0; i < Enum.GetNames<FilterTagAction>().Length; i++) {
                _controlButtons[i] = new() {
                    CornerRadius = radius,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    BorderBrush = _controlButtonBorderColors[i],
                    Content = new TextBlock() {
                        Text = _controlButtonTexts[i],
                        TextWrapping = TextWrapping.WrapWholeWords
                    }
                };
                ControlButtonContainer.RowDefinitions.Add(new());
                Grid.SetRow(_controlButtons[i], i);
                ControlButtonContainer.Children.Add(_controlButtons[i]);
            }
            void SetConfirmDialogXamlRoot(object _0, RoutedEventArgs _1) {
                _confirmDialog.XamlRoot = XamlRoot;
                Loaded -= SetConfirmDialogXamlRoot;
            };
            Loaded += SetConfirmDialogXamlRoot;

            _controlButtons[(int)FilterTagAction.Create].Click += CreateTagBtn_Clicked;
            _controlButtons[(int)FilterTagAction.Rename].Click += RenameTagBtn_Clicked;
            _controlButtons[(int)FilterTagAction.Save].Click += SaveTagBtn_Clicked;
            _controlButtons[(int)FilterTagAction.Delete].Click += DeleteTagBtn_Clicked;
            _controlButtons[(int)FilterTagAction.Clear].Click += ClearTagTextboxBtn_Clicked;
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

        private async void CreateTagBtn_Clicked(object _0, RoutedEventArgs _1) {
            string newTagName = TagNameTextBox.Text.Trim();
            if (newTagName.Length == 0) {
               MainWindow.NotifyUser("No Tag Name", "Please enter a tag name");
                return;
            }
            if (_tagFilterDict.ContainsKey(newTagName)) {
               MainWindow.NotifyUser("Duplicate Tag Name", "A tag list with the name already exists");
                return;
            }
            string overlappingTagsText = GetCurrTagFilter().GetIncludeExcludeOverlap();
            if (overlappingTagsText != "") {
               MainWindow.NotifyUser("The following tags are overlapping in Include and Exclude tags", overlappingTagsText);
                return;
            }
            ContentDialogResult cdr = await ShowConfirmDialogAsync($"Create '{newTagName}'?", "");
            if (cdr != ContentDialogResult.Primary) {
                return;
            }
            TagNameTextBox.Text = "";
            AddToTagsDict(newTagName, GetCurrTagFilter());
            FilterTagComboBox.SelectedItem = newTagName;
            WriteObjectToJson(TAG_FILTERS_FILE_PATH, _tagFilterDict);
            MainWindow.NotifyUser($"'{newTagName}' created", "");
        }

        private async void RenameTagBtn_Clicked(object _0, RoutedEventArgs _1) {
            if (_currTagName == null) return;
            string oldTagName = _currTagName;
            string newTagName = TagNameTextBox.Text.Trim();
            if (newTagName.Length == 0) {
                MainWindow.NotifyUser("No Tag Name", "Please enter a tag name");
                return;
            }
            if (_tagFilterDict.ContainsKey(newTagName)) {
                MainWindow.NotifyUser("Duplicate Tag Name", "A tag list with the name already exists");
                return;
            }
            ContentDialogResult cdr = await ShowConfirmDialogAsync($"Rename '{oldTagName}' to '{newTagName}'?", "");
            if (cdr != ContentDialogResult.Primary) {
                return;
            }
            TagNameTextBox.Text = "";
            AddToTagsDict(newTagName, _tagFilterDict[oldTagName]);
            FilterTagComboBox.SelectedItem = newTagName;
            RemoveFromTagsDict(oldTagName);
            WriteObjectToJson(TAG_FILTERS_FILE_PATH, _tagFilterDict);
           MainWindow.NotifyUser($"'{oldTagName}' renamed to '{newTagName}'", "");
        }

        private async void SaveTagBtn_Clicked(object _0, RoutedEventArgs _1) {
            if (_currTagName == null) return;
            string overlappingTagsText = GetCurrTagFilter().GetIncludeExcludeOverlap();
            if (overlappingTagsText != "") {
               MainWindow.NotifyUser("The following tags are overlapping in Include and Exclude tags", overlappingTagsText);
                return;
            }
            ContentDialogResult cdr = await ShowConfirmDialogAsync($"Save current tags on '{_currTagName}'?", $"'{_currTagName}' will be overwritten.");
            if (cdr != ContentDialogResult.Primary) {
                return;
            }
            _tagFilterDict[_currTagName] = GetCurrTagFilter();
            WriteObjectToJson(TAG_FILTERS_FILE_PATH, _tagFilterDict);
           MainWindow.NotifyUser($"'{_currTagName}' saved", "");
        }

        private async void DeleteTagBtn_Clicked(object _0, RoutedEventArgs _1) {
            if (_currTagName == null) return;
            string oldTagName = _currTagName;
            ContentDialogResult cdr = await ShowConfirmDialogAsync($"Delete '{oldTagName}'?", "");
            if (cdr != ContentDialogResult.Primary) {
                return;
            }
            RemoveFromTagsDict(oldTagName);
            WriteObjectToJson(TAG_FILTERS_FILE_PATH, _tagFilterDict);
           MainWindow.NotifyUser($"'{oldTagName}' deleted", "");
        }

        private void ClearTagTextboxBtn_Clicked(object _0, RoutedEventArgs _1) {
            IncludeTagContainer.Clear();
            ExcludeTagContainer.Clear();
        }

        private void FilterTagComboBox_SelectionChanged(object _0, SelectionChangedEventArgs _1) {
            _currTagName = (string)FilterTagComboBox.SelectedItem;
            if (_currTagName == null) {
                ClearTagTextboxBtn_Clicked(null, null);
                return;
            }
            TagFilter tag = _tagFilterDict[_currTagName];
            IncludeTagContainer.InsertTags(tag.includeTags);
            ExcludeTagContainer.InsertTags(tag.excludeTags);
        }

        private TagFilter GetCurrTagFilter() {
            return new TagFilter() {
                includeTags = IncludeTagContainer.GetTags(),
                excludeTags = ExcludeTagContainer.GetTags()
            };
        }

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

            string overlappingTagsText = combinedTagFilter.GetIncludeExcludeOverlap();
            if (overlappingTagsText != "") {
               MainWindow.NotifyUser("The following tags are overlapping in Include and Exclude tags", overlappingTagsText);
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
               MainWindow.NotifyUser("All selected tags are empty", "");
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

        private void DownloadBtn_Clicked(object _0, RoutedEventArgs _1) {
            string idPattern = @"\d{" + GALLERY_ID_LENGTH_RANGE.Start + "," + GALLERY_ID_LENGTH_RANGE.End + "}";
            string[] urlOrIds = GalleryIDTextBox.Text.Split(NEW_LINE_SEPS, STR_SPLIT_OPTION);
            if (urlOrIds.Length == 0) {
               MainWindow.NotifyUser("Please enter an ID(s) or an URL(s)", "");
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
               MainWindow.NotifyUser("Invalid ID(s) or URL(s)", "Please enter valid ID(s) or URL(s)");
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
