using Hitomi_Scroll_Viewer.SearchPageComponent;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
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
using static Hitomi_Scroll_Viewer.SearchTag;
using static Hitomi_Scroll_Viewer.Utils;

namespace Hitomi_Scroll_Viewer {
    public sealed partial class SearchPage : Page {
        private static readonly string BM_INFO_PATH = Path.Combine(ROOT_DIR, "bookmarkInfo.json");
        private static readonly string TAGS_PATH = Path.Combine(ROOT_DIR, "search_tags.json");

        private static readonly string SEARCH_ADDRESS = "https://hitomi.la/search.html?";
        private static readonly Range GALLERY_ID_LENGTH_RANGE = 6..7;

        private static readonly string GLOBAL_TAG_NAME = "Global";

        private readonly ObservableConcurrentDictionary<string, SearchTag> _tagsDict;
        private static readonly int MAX_BOOKMARK_PER_PAGE = 3;

        private static readonly List<BookmarkItem> bmItems = [];
        private static readonly object _bmLock = new();

        public enum BookmarkSwapDirection {
            Up, Down
        }

        private static readonly DataPackage _myDataPackage = new() {
            RequestedOperation = DataPackageOperation.Copy
        };

        private readonly ObservableCollection<DownloadItem> _downloadingItems = [];
        public readonly ConcurrentDictionary<string, byte> downloadingGalleries = new();

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
            CloseButtonText = "Cancel",
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

            if (File.Exists(TAGS_PATH)) {
                // read tag info from file
                _tagsDict = (ObservableConcurrentDictionary<string, SearchTag>)JsonSerializer.Deserialize(
                    File.ReadAllText(TAGS_PATH),
                    typeof(ObservableConcurrentDictionary<string, SearchTag>),
                    serializerOptions
                    );
            } else {
                // if tag file doesn't exist, initialise _tags with Global tag list
                _tagsDict = new() {
                    { GLOBAL_TAG_NAME, new() }
                };
                WriteObjectToJson(TAGS_PATH, _tagsDict);
            }

            foreach (KeyValuePair<string, SearchTag> item in _tagsDict) {
                TagListComboBox.Items.Add(item.Key);
            }
            TagListComboBox.SelectedIndex = 0;

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
            BookmarkPageSelector.SelectionChanged += (_, _) => {
                UpdateBookmark();
            };

            // fill bookmarks
            for (int i = 0; i < galleries.Count; i++) {
                bmItems.Add(new(galleries[i], this, true));
            }
            UpdateBookmark();
        }

        private void InitLayout() {
            // Create Tag Control Buttons
            CornerRadius radius = new(8);
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

            _controlButtons[(int)TagListAction.Create].Click += CreateTag;
            _controlButtons[(int)TagListAction.Rename].Click += RenameTag;
            _controlButtons[(int)TagListAction.Save].Click += SaveTag;
            _controlButtons[(int)TagListAction.Remove].Click += RemoveTag;
            _controlButtons[(int)TagListAction.Clear].Click += ClearTagTextbox;

            // set hyperlink panel max height
            void setHyperlinkPanelHeight(object _0, SizeChangedEventArgs _1) {
                ((ScrollViewer)(HyperlinkPanel.Parent)).MaxHeight = TagControlGrid.ActualHeight - GenerateHyperlinkBtn.ActualHeight - AddressControlPanel.Spacing;
                AddressControlPanel.MaxHeight = TagControlGrid.ActualHeight;
                TagControlGrid.SizeChanged -= setHyperlinkPanelHeight;
            };
            TagControlGrid.SizeChanged += setHyperlinkPanelHeight;
        }

        private async Task<ContentDialogResult> ShowConfirmDialogAsync(string title, string content) {
            _confirmDialog.Title = title;
            _confirmDialog.Content = content;
            ContentDialogResult result = await _confirmDialog.ShowAsync();
            return result;
        }

        private async void CreateTag(object _0, RoutedEventArgs _1) {
            string newTagName = TagNameTextBox.Text.Trim();
            if (newTagName.Length == 0) {
                _mw.NotifyUser("No Tag Name", "Please enter a tag name");
                return;
            }
            if (_tagsDict.ContainsKey(newTagName)) {
                _mw.NotifyUser("Duplicate Tag Name", "A tag list with the name already exists");
                return;
            }
            string overlappingTagsText = GetCurrTag().GetIncludeExcludeOverlap();
            if (overlappingTagsText != "") {
                _mw.NotifyUser("The following tags are overlapping in Include and Exclude tags", overlappingTagsText);
                return;
            }
            ContentDialogResult cdr = await ShowConfirmDialogAsync($"Create '{newTagName}'?", "");
            if (cdr != ContentDialogResult.Primary) {
                return;
            }
            TagNameTextBox.Text = "";
            _tagsDict.Add(newTagName, GetCurrTag());
            TagListComboBox.Items.Add(newTagName);
            TagListComboBox.SelectedItem = newTagName;
            WriteObjectToJson(TAGS_PATH, _tagsDict);
            _mw.NotifyUser($"'{newTagName}' has been created", "");
        }

        private async void RenameTag(object _0, RoutedEventArgs _1) {
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
            _tagsDict.Add(newTagName, _tagsDict[oldTagName]);
            TagListComboBox.Items.Add(newTagName);
            _tagsDict.Remove(oldTagName);
            TagListComboBox.SelectedItem = newTagName;
            TagListComboBox.Items.Remove(oldTagName);
            WriteObjectToJson(TAGS_PATH, _tagsDict);
            _mw.NotifyUser($"'{oldTagName}' has been renamed to '{newTagName}'", "");
        }

        private async void SaveTag(object _0, RoutedEventArgs _1) {
            string overlappingTagsText = GetCurrTag().GetIncludeExcludeOverlap();
            if (overlappingTagsText != "") {
                _mw.NotifyUser("The following tags are overlapping in Include and Exclude tags", overlappingTagsText);
                return;
            }
            ContentDialogResult cdr = await ShowConfirmDialogAsync($"Save current tags on '{_currTagName}'?", $"'{_currTagName}' will be overwritten.");
            if (cdr != ContentDialogResult.Primary) {
                return;
            }
            _tagsDict[_currTagName] = GetCurrTag();
            WriteObjectToJson(TAGS_PATH, _tagsDict);
            _mw.NotifyUser($"'{_currTagName}' has been saved", "");
        }

        private async void RemoveTag(object _0, RoutedEventArgs _1) {
            string oldTagName = _currTagName;
            ContentDialogResult cdr = await ShowConfirmDialogAsync($"Remove '{oldTagName}'?", "");
            if (cdr != ContentDialogResult.Primary) {
                return;
            }
            _tagsDict.Remove(oldTagName);
            TagListComboBox.Items.Remove(oldTagName);
            TagListComboBox.SelectedIndex = 0;
            WriteObjectToJson(TAGS_PATH, _tagsDict);
            _mw.NotifyUser($"'{oldTagName}' has been removed", "");
        }

        private async void ClearTagTextbox(object _0, RoutedEventArgs _1) {
            ContentDialogResult cdr = await ShowConfirmDialogAsync("Clear all texts in tag containers?", "");
            if (cdr != ContentDialogResult.Primary) {
                return;
            }
            IncludeTagContainer.Clear();
            ExcludeTagContainer.Clear();
        }

        private void LoadTagsInTextBox(object sender, SelectionChangedEventArgs _1) {
            ComboBox cb = (ComboBox)sender;
            if (cb.SelectedItem == null) {
                cb.SelectedIndex = 0;
            }
            _currTagName = (string)cb.SelectedItem;
            // disable rename and remove button if global tag is selected
            if (_currTagName == GLOBAL_TAG_NAME) {
                _controlButtons[(int)TagListAction.Rename].IsEnabled = false;
                _controlButtons[(int)TagListAction.Remove].IsEnabled = false;
            }
            else {
                _controlButtons[(int)TagListAction.Rename].IsEnabled = true;
                _controlButtons[(int)TagListAction.Remove].IsEnabled = true;
            }
            SearchTag tag = _tagsDict[_currTagName];
            IncludeTagContainer.InsertTags(tag.includeTags);
            ExcludeTagContainer.InsertTags(tag.excludeTags);
        }

        public string[] GetGlobalTag(string tagCategory, bool isInclude) {
            if ((bool)IgnoreGlobalTagBtn.IsChecked) {
                return [];
            }
            if (isInclude) {
                return _tagsDict[GLOBAL_TAG_NAME].includeTags[tagCategory];
            }
            return _tagsDict[GLOBAL_TAG_NAME].excludeTags[tagCategory];
        }

        private SearchTag GetCurrTag() {
            return new SearchTag() {
                includeTags = IncludeTagContainer.GetTags(),
                excludeTags = ExcludeTagContainer.GetTags()
            };
        }

        private void GenerateHyperlink(object _0, RoutedEventArgs _1) {
            string address = SEARCH_ADDRESS + string.Join(
                ' ',
                CATEGORIES.Select((category, idx) => 
                    IncludeTagContainer.GetSearchParameters(idx, GetGlobalTag(category, true)) + ' ' +
                    ExcludeTagContainer.GetSearchParameters(idx, GetGlobalTag(category, false))
                ).Where(searchParam => !string.IsNullOrWhiteSpace(searchParam))
            );
            // copy link to clipboard
            _myDataPackage.SetText(address);
            Clipboard.SetContent(_myDataPackage);

            Grid gridItem = new() {
                ColumnDefinitions = {
                    new ColumnDefinition() { Width = new GridLength(7, GridUnitType.Star) },
                    new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) },
                },
            };

            HyperlinkButton hb = new() {
                Content = new TextBlock() {
                    Text = string.Join(
                        Environment.NewLine,
                        CATEGORIES.Select((category, idx) => {
                            string displayTagTexts =
                                IncludeTagContainer.GetHyperlinkDisplayTexts(idx, GetGlobalTag(category, true)) + ' ' +
                                ExcludeTagContainer.GetHyperlinkDisplayTexts(idx, GetGlobalTag(category, false));
                            if (!string.IsNullOrWhiteSpace(displayTagTexts)) {
                                return char.ToUpper(category[0]) + category[1..] + ": " + displayTagTexts;
                            } else {
                                return "";
                            }
                        }).Where((displayTagTexts) => displayTagTexts != "")
                    ),
                    TextWrapping = TextWrapping.WrapWholeWords,
                    FontSize = 10,
                },
                NavigateUri = new Uri(address),
                HorizontalAlignment = HorizontalAlignment.Stretch,
            };

            Grid.SetColumn(hb, 0);
            gridItem.Children.Add(hb);
            
            Button removeBtn = new() {
                Content = new TextBlock() {
                    Text = "Remove",
                    TextWrapping = TextWrapping.WrapWholeWords,
                },
                FontSize = 12,
            };
            removeBtn.Click += (object sender, RoutedEventArgs _) => {
                HyperlinkPanel.Children.Remove((Grid)((Button)sender).Parent);
            };
            Grid.SetColumn(removeBtn, 1);
            gridItem.Children.Add(removeBtn);
            
            HyperlinkPanel.Children.Add(gridItem);
        }

        private void HandleDownloadBtnClick(object _0, RoutedEventArgs _1) {
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
                _downloadingItems.Add(new DownloadItem(extractedId, _mw.httpClient, this, _downloadingItems));
            }
        }

        public static void EnableBookmarkLoading(bool enable) {
            for (int i = 0; i < bmItems.Count; i++) {
                bmItems[i].EnableBookmarkLoading(enable);
            }
        }

        public static void LoadBookmark(Gallery gallery) {
            _mw.SwitchPage();

            // if the already loaded gallery is the same gallery, just return
            if (_mw.CurrLoadedGallery != null) {
                if (gallery.id == _mw.CurrLoadedGallery.id) {
                    _mw.NotifyUser("The gallery is already loaded", "");
                    return;
                }
            }
            _mw.Iwp.LoadGalleryFromLocalDir(gallery);
        }

        public BookmarkItem AddBookmark(Gallery gallery, bool tryLoadingImages) {
            lock (_bmLock) {
                // return the BookmarkItem if it is already bookmarked
                var bmItem = GetBookmarkItem(gallery.id);
                if (bmItem != null) {
                    return bmItem;
                }
                bmItem = new BookmarkItem(gallery, this, tryLoadingImages);
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
