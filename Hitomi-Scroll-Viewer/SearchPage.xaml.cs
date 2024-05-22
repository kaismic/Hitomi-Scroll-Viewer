using Hitomi_Scroll_Viewer.SearchPageComponent;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using static Hitomi_Scroll_Viewer.SearchTag;
using static Hitomi_Scroll_Viewer.Utils;

namespace Hitomi_Scroll_Viewer {
    public sealed partial class SearchPage : Page {
        private static readonly string BM_INFO_PATH = Path.Combine(ROOT_DIR, "bookmarkInfo.json");
        private static readonly string TAGS_PATH = Path.Combine(ROOT_DIR, "search_tags.json");

        private static readonly string SEARCH_ADDRESS = "https://hitomi.la/search.html?";
        private static readonly Range GALLERY_ID_LENGTH_RANGE = 6..7;

        private readonly Dictionary<string, SearchTag> _tagsDict;
        private readonly ObservableCollection<string> _tagsDictKeys;
        private readonly ObservableCollection<SearchFilterItem> _searchFilterItems;

        private static readonly int MAX_BOOKMARK_PER_PAGE = 3;

        private static readonly List<BookmarkItem> bmItems = [];
        private static readonly object _bmLock = new();

        public enum BookmarkSwapDirection {
            Up, Down
        }

        private static readonly DataPackage _myDataPackage = new() {
            RequestedOperation = DataPackageOperation.Copy
        };

        private readonly ObservableCollection<SearchLinkItem> _searchLinkItems = [];

        private readonly ObservableCollection<DownloadItem> _downloadingItems = [];
        public readonly ConcurrentDictionary<string, byte> downloadingGalleries = [];

        private string _currTagName;

        private enum TagListAction {
            Create,
            Rename,
            Save,
            Remove,
            Clear
        }

        private readonly ContentDialog _confirmDialog = new() {
            IsPrimaryButtonEnabled = true,
            PrimaryButtonText = "Yes",
            CloseButtonText = "Cancel"
        };
        private readonly Button[] _controlButtons = new Button[Enum.GetNames<TagListAction>().Length];
        private readonly string[] _controlButtonTexts = [
            "Create a new tag list",
            "Rename current tag list",
            "Save current tag list",
            "Remove current tag list",
            "Clear texts in tag containers",
        ];
        private readonly SolidColorBrush[] _controlButtonBorderColors = [
            new(Colors.Blue),
            new(Colors.Orange),
            new(Colors.Green),
            new(Colors.Red),
            new(Colors.Black)
        ];

        private static MainWindow _mw;

        public SearchPage(MainWindow mainWindow) {
            InitializeComponent();
            InitLayout();
            _mw = mainWindow;

            _tagsDict = File.Exists(TAGS_PATH)
                ? (Dictionary<string, SearchTag>)JsonSerializer.Deserialize(
                    File.ReadAllText(TAGS_PATH),
                    typeof(Dictionary<string, SearchTag>),
                    serializerOptions
                )
                : new() {
                    { "Tag1", new() },
                    { "Tag2", new() },
                    { "Tag3", new() },
                };

            _tagsDictKeys = new(_tagsDict.Keys);
            _searchFilterItems = new(_tagsDict.Keys.Select(key => new SearchFilterItem(key, TagNameCheckBox_StateChanged)));

            // create bookmarked galleries' info file if it doesn't exist
            if (!File.Exists(BM_INFO_PATH)) {
                WriteObjectToJson(BM_INFO_PATH, new List<Gallery>());
            }
            // read bookmarked galleries' info from file
            List<Gallery> galleries = (List<Gallery>)JsonSerializer.Deserialize(
                File.ReadAllText(BM_INFO_PATH),
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
                bmItems.Add(new(gallery, this, false));
            }
            UpdateBookmark();
        }

        private void InitLayout() {
            // Create Tag Control Buttons
            CornerRadius radius = new(4);
            for (int i = 0; i < Enum.GetNames<TagListAction>().Length; i++) {
                _controlButtons[i] = new() {
                    MinHeight = 80,
                    CornerRadius = radius,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    BorderBrush = _controlButtonBorderColors[i],
                    Content = new TextBlock() {
                        Text = _controlButtonTexts[i],
                        TextWrapping = TextWrapping.WrapWholeWords
                    }
                };
                ControlButtonContainer.Children.Add(_controlButtons[i]);
            }
            void setXamlRoot(object _0, RoutedEventArgs _1) {
                _confirmDialog.XamlRoot = XamlRoot;
                Loaded -= setXamlRoot;
            };
            Loaded += setXamlRoot;

            _controlButtons[(int)TagListAction.Create].Click += CreateTagBtn_Clicked;
            _controlButtons[(int)TagListAction.Rename].Click += RenameTagBtn_Clicked;
            _controlButtons[(int)TagListAction.Save].Click += SaveTagBtn_Clicked;
            _controlButtons[(int)TagListAction.Remove].Click += RemoveTagBtn_Clicked;
            _controlButtons[(int)TagListAction.Clear].Click += ClearTagTextboxBtn_Clicked;

            void SetActionPanelsHeight(object _0, RoutedEventArgs _1) {
                SearchLinkItemsListView.MaxHeight = ControlButtonContainer.ActualHeight - CreateHyperlinkBtn.ActualHeight - (SearchLinkItemsListView.Parent as StackPanel).Spacing;
                DownloadItemsListView.MaxHeight = ControlButtonContainer.ActualHeight - DownloadActionGrid.ActualHeight - (DownloadItemsListView.Parent as StackPanel).Spacing;
                ControlButtonContainer.Loaded -= SetActionPanelsHeight;
            };
            ControlButtonContainer.Loaded += SetActionPanelsHeight;

        }

        private async Task<ContentDialogResult> ShowConfirmDialogAsync(string title, string content) {
            _confirmDialog.Title = title;
            _confirmDialog.Content = content;
            ContentDialogResult result = await _confirmDialog.ShowAsync();
            return result;
        }

        private void TagNameCheckBox_StateChanged(object sender, RoutedEventArgs e) {
            CreateHyperlinkBtn.IsEnabled = _searchFilterItems.Any(item => (bool)item.IsChecked);
        }

        private void AddToTagsDict(string tagName, SearchTag searchTag) {
            _tagsDict.Add(tagName, searchTag);
            _tagsDictKeys.Add(tagName);
            _searchFilterItems.Add(new(tagName, TagNameCheckBox_StateChanged));
        }
        
        private void RemoveFromTagsDict(string tagName) {
            _tagsDict.Remove(tagName);
            _tagsDictKeys.Remove(tagName);
            _searchFilterItems.Remove(_searchFilterItems.First(item => item.TagName == tagName));
            CreateHyperlinkBtn.IsEnabled = _searchFilterItems.Count != 0;
        }

        private async void CreateTagBtn_Clicked(object _0, RoutedEventArgs _1) {
            string newTagName = TagNameTextBox.Text.Trim();
            if (newTagName.Length == 0) {
                _mw.NotifyUser("No Tag Name", "Please enter a tag name");
                return;
            }
            if (_tagsDict.ContainsKey(newTagName)) {
                _mw.NotifyUser("Duplicate Tag Name", "A tag list with the name already exists");
                return;
            }
            string overlappingTagsText = GetCurrSearchTag().GetIncludeExcludeOverlap();
            if (overlappingTagsText != "") {
                _mw.NotifyUser("The following tags are overlapping in Include and Exclude tags", overlappingTagsText);
                return;
            }
            ContentDialogResult cdr = await ShowConfirmDialogAsync($"Create '{newTagName}'?", "");
            if (cdr != ContentDialogResult.Primary) {
                return;
            }
            TagNameTextBox.Text = "";
            AddToTagsDict(newTagName, GetCurrSearchTag());
            TagListComboBox.SelectedItem = newTagName;
            WriteObjectToJson(TAGS_PATH, _tagsDict);
            _mw.NotifyUser($"'{newTagName}' created", "");
        }

        private async void RenameTagBtn_Clicked(object _0, RoutedEventArgs _1) {
            if (_currTagName == null) return;
            string oldTagName = _currTagName;
            string newTagName = TagNameTextBox.Text.Trim();
            if (newTagName.Length == 0) {
                _mw.NotifyUser("No Tag Name", "Please enter a tag name");
                return;
            }
            if (_tagsDict.ContainsKey(newTagName)) {
                _mw.NotifyUser("Duplicate Tag Name", "A tag list with the name already exists");
                return;
            }
            ContentDialogResult cdr = await ShowConfirmDialogAsync($"Rename '{oldTagName}' to '{newTagName}'?", "");
            if (cdr != ContentDialogResult.Primary) {
                return;
            }
            TagNameTextBox.Text = "";
            AddToTagsDict(newTagName, _tagsDict[oldTagName]);
            TagListComboBox.SelectedItem = newTagName;
            RemoveFromTagsDict(oldTagName);
            WriteObjectToJson(TAGS_PATH, _tagsDict);
            _mw.NotifyUser($"'{oldTagName}' renamed to '{newTagName}'", "");
        }

        private async void SaveTagBtn_Clicked(object _0, RoutedEventArgs _1) {
            if (_currTagName == null) return;
            string overlappingTagsText = GetCurrSearchTag().GetIncludeExcludeOverlap();
            if (overlappingTagsText != "") {
                _mw.NotifyUser("The following tags are overlapping in Include and Exclude tags", overlappingTagsText);
                return;
            }
            ContentDialogResult cdr = await ShowConfirmDialogAsync($"Save current tags on '{_currTagName}'?", $"'{_currTagName}' will be overwritten.");
            if (cdr != ContentDialogResult.Primary) {
                return;
            }
            _tagsDict[_currTagName] = GetCurrSearchTag();
            WriteObjectToJson(TAGS_PATH, _tagsDict);
            _mw.NotifyUser($"'{_currTagName}' saved", "");
        }

        private async void RemoveTagBtn_Clicked(object _0, RoutedEventArgs _1) {
            if (_currTagName == null) return;
            string oldTagName = _currTagName;
            ContentDialogResult cdr = await ShowConfirmDialogAsync($"Remove '{oldTagName}'?", "");
            if (cdr != ContentDialogResult.Primary) {
                return;
            }
            RemoveFromTagsDict(oldTagName);
            WriteObjectToJson(TAGS_PATH, _tagsDict);
            _mw.NotifyUser($"'{oldTagName}' removed", "");
        }

        private void ClearTagTextboxBtn_Clicked(object _0, RoutedEventArgs _1) {
            IncludeTagContainer.Clear();
            ExcludeTagContainer.Clear();
        }

        private void TagListComboBox_SelectionChanged(object sender, SelectionChangedEventArgs _1) {
            _currTagName = (string)TagListComboBox.SelectedItem;
            if (_currTagName == null) {
                ClearTagTextboxBtn_Clicked(null, null);
                return;
            }
            SearchTag tag = _tagsDict[_currTagName];
            IncludeTagContainer.InsertTags(tag.includeTags);
            ExcludeTagContainer.InsertTags(tag.excludeTags);
        }

        private SearchTag GetCurrSearchTag() {
            return new SearchTag() {
                includeTags = IncludeTagContainer.GetTags(),
                excludeTags = ExcludeTagContainer.GetTags()
            };
        }

        private void CreateHyperlinkBtn_Clicked(object _0, RoutedEventArgs _1) {
            var selectedSearchFilterItems = _searchFilterItems.Where(item => (bool)item.IsChecked);
            SearchTag combinedSearchTag = selectedSearchFilterItems.Aggregate(
                new SearchTag(),
                (result, item) => {
                    SearchTag currSearchTag = _tagsDict[item.TagName];
                    foreach (string category in CATEGORIES) {
                        result.includeTags[category] = result.includeTags[category].Union(currSearchTag.includeTags[category]).ToHashSet();
                        result.excludeTags[category] = result.excludeTags[category].Union(currSearchTag.excludeTags[category]).ToHashSet();
                    }
                    return result;
                }
            );

            string overlappingTagsText = combinedSearchTag.GetIncludeExcludeOverlap();
            if (overlappingTagsText != "") {
                _mw.NotifyUser("The following tags are overlapping in Include and Exclude tags", overlappingTagsText);
                return;
            }

            string searchParams = string.Join(
                ' ',
                CATEGORIES.Select(category =>
                    (
                        string.Join(' ', combinedSearchTag.includeTags[category].Select(tag => category + ':' + tag.Replace(' ', '_')))
                            + ' ' +
                        string.Join(' ', combinedSearchTag.excludeTags[category].Select(tag => '-' + category + ':' + tag.Replace(' ', '_')))
                    ).Trim()
                ).Where(searchParam => searchParam != "")
            );
            if (searchParams == "") {
                _mw.NotifyUser("All selected tags are empty", "");
                return;
            }
            string searchLink = SEARCH_ADDRESS + searchParams;

            string displayText = string.Join(
                Environment.NewLine,
                CATEGORIES.Select(category => {
                    string displayTextPart =
                        (
                            string.Join(' ', combinedSearchTag.includeTags[category].Select(tag => tag.Replace(' ', '_')))
                            + ' ' +
                            string.Join(' ', combinedSearchTag.excludeTags[category].Select(tag => '-' + tag.Replace(' ', '_')))
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
                _mw.NotifyUser("Please enter an ID(s) or an URL(s)", "");
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
                _mw.NotifyUser("Invalid ID(s) or URL(s)", "Please enter valid ID(s) or URL(s)");
                return;
            }
            GalleryIDTextBox.Text = "";
            
            foreach (string extractedId in extractedIds) {
                // skip if it is already downloading
                if (downloadingGalleries.TryGetValue(extractedId, out _)) {
                    // TODO toast message "Gallery {extractedId} is currently being downloaded"
                    continue;
                }
                // Download
                downloadingGalleries.TryAdd(extractedId, 0);
                _downloadingItems.Add(new (extractedId, _mw.httpClient, this, _downloadingItems));
            }
        }

        public static void EnableBookmarkLoading(bool enable) {
            for (int i = 0; i < bmItems.Count; i++) {
                bmItems[i].EnableClick(enable);
            }
        }

        public static void LoadBookmark(Gallery gallery) {
            _mw.SwitchPage();
            if (_mw.CurrLoadedGallery != null && gallery.id == _mw.CurrLoadedGallery.id) {
                return;
            }
            _mw.LoadGallery(gallery);
        }

        public BookmarkItem CreateAndAddBookmark(Gallery gallery) {
            lock (_bmLock) {
                // return the BookmarkItem if it is already bookmarked
                var bmItem = GetBookmarkItem(gallery.id);
                if (bmItem != null) {
                    return bmItem;
                }
                bmItem = new BookmarkItem(gallery, this, true);
                bmItems.Add(bmItem);
                // new page is needed
                if (bmItems.Count % MAX_BOOKMARK_PER_PAGE == 1) {
                    BookmarkPageSelector.Items.Add(BookmarkPageSelector.Items.Count);
                }
                WriteObjectToJson(BM_INFO_PATH, bmItems.Select(bmItem => bmItem.gallery));
                if (bmItems.Count == 1) {
                    BookmarkPageSelector.SelectedIndex = 0;
                } else {
                    UpdateBookmark();
                }
                return bmItem;
            }
        }

        public void RemoveBookmark(BookmarkItem bmItem) {
            lock (_bmLock) {
                string path = Path.Combine(IMAGE_DIR, bmItem.gallery.id);
                if (Directory.Exists(path)) Directory.Delete(path, true);
                bmItems.Remove(bmItem);
                WriteObjectToJson(BM_INFO_PATH, bmItems.Select(bmItem => bmItem.gallery));

                bool pageChanged = false;
                // a page needs to be removed
                if (bmItems.Count % MAX_BOOKMARK_PER_PAGE == 0) {
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
                        (bmItems[idx], bmItems[idx - 1]) = (bmItems[idx - 1], bmItems[idx]);
                        break;
                    }
                    case BookmarkSwapDirection.Down: {
                        if (idx == bmItems.Count - 1) {
                            return;
                        }
                        (bmItems[idx], bmItems[idx + 1]) = (bmItems[idx + 1], bmItems[idx]);
                        break;
                    }
                }
                WriteObjectToJson(BM_INFO_PATH, bmItems.Select(bmItem => bmItem.gallery));
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
                if (i < bmItems.Count) {
                    BookmarkPanel.Children.Add(bmItems[i]);
                }
            }
        }

        /**
         * <returns>The <c>Gallery</c> if the gallery with the given id is bookmarked, otherwise <c>null</c>.</returns>
         */
        public static BookmarkItem GetBookmarkItem(string id) {
            for (int i = 0; i < bmItems.Count; i++) {
                if (bmItems[i].gallery.id == id) {
                    return bmItems[i];
                }
            }
            return null;
        }

        /**
         * <returns>The bookmark index of the bookmarked gallery. The given id must be from a bookmarked gallery.</returns>
         */
        public static int GetBookmarkIndex(string id) {
            for (int i = 0; i < bmItems.Count; i++) {
                if (bmItems[i].gallery.id == id) {
                    return i;
                }
            }
            throw new ArgumentException("Id must be from a bookmarked gallery.");
        }
    }
}
