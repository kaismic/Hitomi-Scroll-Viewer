using Hitomi_Scroll_Viewer.SearchPageComponent;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using Windows.ApplicationModel.DataTransfer;
using static Hitomi_Scroll_Viewer.ImageWatchingPage;
using static Hitomi_Scroll_Viewer.Tag;
using static Hitomi_Scroll_Viewer.Utils;

namespace Hitomi_Scroll_Viewer {
    public sealed partial class SearchPage : Page {
        private static readonly string BM_INFO_PATH = Path.Combine(ROOT_DIR, "bookmarkInfo.json");
        private static readonly string TAGS_PATH = Path.Combine(ROOT_DIR, "tags.json");

        private static readonly string SEARCH_ADDRESS = "https://hitomi.la/search.html?";
        private static readonly Range GALLERY_ID_LENGTH_RANGE = 6..7;

        private static readonly string GLOBAL_TAG_NAME = "Global";

        public static Dictionary<string, Tag> Tags { get; set; }

        public static readonly double THUMBNAIL_IMG_HEIGHT = 256;
        public static readonly int THUMBNAIL_IMG_NUM = 3;
        public static readonly int MAX_BOOKMARK_PER_PAGE = 3;

        private static readonly List<BookmarkItem> bmItems = [];
        private static readonly object _bmLock = new();

        public enum BookmarkSwapDirection {
            Up, Down
        }

        private static readonly DataPackage _myDataPackage = new() {
            RequestedOperation = DataPackageOperation.Copy
        };

        public readonly ConcurrentBag<string> DownloadingGalleries = new();
        

        private string _currTagName;

        public enum TagListAction {
            Create,
            Rename,
            Save,
            Remove,
            Clear
        }

        private readonly ContentDialog[] _confirmDialogs = new ContentDialog[Enum.GetNames<TagListAction>().Length];
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
        private static ImageWatchingPage _iwp;

        public SearchPage(MainWindow mainWindow) {
            InitializeComponent();
            InitLayout();
            _mw = mainWindow;

            if (File.Exists(TAGS_PATH)) {
                // read tag info from file
                Tags = (Dictionary<string, Tag>)JsonSerializer.Deserialize(
                    File.ReadAllText(TAGS_PATH),
                    typeof(Dictionary<string, Tag>),
                    serializerOptions
                    );
            } else {
                //  if tag file doesn't exist, initialise _tags with Global tag list
                Tags = new() {
                    { GLOBAL_TAG_NAME, new() }
                };
                File.WriteAllText(TAGS_PATH, JsonSerializer.Serialize(Tags, serializerOptions));
            }

            foreach (KeyValuePair<string, Tag> item in Tags) {
                TagListComboBox.Items.Add(item.Key);
            }
            TagListComboBox.SelectedIndex = 0;

            // create bookmarked galleries' info file if it doesn't exist
            if (!File.Exists(BM_INFO_PATH)) {
                File.WriteAllText(BM_INFO_PATH, JsonSerializer.Serialize(new List<Gallery>(), serializerOptions));
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
                FillBookmark();
            };

            // fill bookmarks
            for (int i = 0; i < galleries.Count; i++) {
                bmItems.Add(new(galleries[i], this));
            }
            FillBookmark();
        }

        public static void Init(ImageWatchingPage iwp) {
            _iwp = iwp;
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
                _confirmDialogs[i] = new() {
                    IsPrimaryButtonEnabled = true,
                    PrimaryButtonText = "Yes",
                    CloseButtonText = "Cancel",
                };
            }
            void setXamlRoot(object _0, RoutedEventArgs _1) {
                for (int i = 0; i < Enum.GetNames<TagListAction>().Length; i++) {
                    _confirmDialogs[i].XamlRoot = XamlRoot;
                }
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

        public static void WriteTag() {
            File.WriteAllText(TAGS_PATH, JsonSerializer.Serialize(Tags, serializerOptions));
        }

        private async void CreateTag(object _0, RoutedEventArgs _1) {
            string newTagName = TagNameTextBox.Text.Trim();
            if (newTagName.Length == 0) {
                _mw.AlertUser("No Tag Name", "Please enter a tag name");
                return;
            }
            if (Tags.ContainsKey(newTagName)) {
                _mw.AlertUser("Duplicate Tag Name", "A tag list with the name already exists");
                return;
            }
            _confirmDialogs[(int)TagListAction.Create].Title = $"Create '{newTagName}'?";
            ContentDialogResult cdr = await _confirmDialogs[(int)TagListAction.Create].ShowAsync();
            if (cdr != ContentDialogResult.Primary) {
                return;
            }
            TagNameTextBox.Text = "";
            Tags.Add(newTagName, GetCurrTag());
            TagListComboBox.Items.Add(newTagName);
            TagListComboBox.SelectedItem = newTagName;
            WriteTag();
            _mw.AlertUser($"'{newTagName}' has been created", "");
        }

        private async void RenameTag(object _0, RoutedEventArgs _1) {
            string oldTagName = _currTagName;
            string newTagName = TagNameTextBox.Text.Trim();
            if (newTagName.Length == 0) {
                _mw.AlertUser("No Tag Name", "Please enter a tag name");
                return;
            }
            if (Tags.ContainsKey(newTagName)) {
                _mw.AlertUser("Duplicate Tag Name", "A tag list with the name already exists");
                return;
            }
            _confirmDialogs[(int)TagListAction.Rename].Title = $"Rename '{oldTagName}' to '{newTagName}'?";
            ContentDialogResult cdr = await _confirmDialogs[(int)TagListAction.Rename].ShowAsync();
            if (cdr != ContentDialogResult.Primary) {
                return;
            }
            TagNameTextBox.Text = "";
            Tags.Add(newTagName, Tags[oldTagName]);
            TagListComboBox.Items.Add(newTagName);
            Tags.Remove(oldTagName);
            TagListComboBox.SelectedItem = newTagName;
            TagListComboBox.Items.Remove(oldTagName);
            WriteTag();
            _mw.AlertUser($"'{oldTagName}' has been renamed to '{newTagName}'", "");
        }

        private async void SaveTag(object _0, RoutedEventArgs _1) {
            _confirmDialogs[(int)TagListAction.Save].Title = $"Save current tags on '{_currTagName}'?";
            _confirmDialogs[(int)TagListAction.Save].Content = $"'{_currTagName}' will be overwritten.";
            ContentDialogResult cdr = await _confirmDialogs[(int)TagListAction.Save].ShowAsync();
            if (cdr != ContentDialogResult.Primary) {
                return;
            }
            Tags[_currTagName] = GetCurrTag();
            WriteTag();
            _mw.AlertUser($"'{_currTagName}' has been saved", "");
        }

        private async void RemoveTag(object _0, RoutedEventArgs _1) {
            string oldTagName = _currTagName;
            _confirmDialogs[(int)TagListAction.Remove].Title = $"Remove '{oldTagName}'?";
            ContentDialogResult cdr = await _confirmDialogs[(int)TagListAction.Remove].ShowAsync();
            if (cdr != ContentDialogResult.Primary) {
                return;
            }
            Tags.Remove(oldTagName);
            TagListComboBox.Items.Remove(oldTagName);
            TagListComboBox.SelectedIndex = 0;
            WriteTag();
            _mw.AlertUser($"'{oldTagName}' has been removed", "");
        }

        private async void ClearTagTextbox(object _0, RoutedEventArgs _1) {
            _confirmDialogs[(int)TagListAction.Clear].Title = "Clear all texts in tag containers?";
            ContentDialogResult cdr = await _confirmDialogs[(int)TagListAction.Clear].ShowAsync();
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
            Tag tag = Tags[_currTagName];
            IncludeTagContainer.InsertTags(tag.includeTags);
            ExcludeTagContainer.InsertTags(tag.excludeTags);
        }

        public string[] GetGlobalTag(string tagCategory, bool isInclude) {
            if ((bool)IgnoreGlobalTagBtn.IsChecked) {
                return [];
            }
            if (isInclude) {
                return Tags[GLOBAL_TAG_NAME].includeTags[tagCategory];
            }
            return Tags[GLOBAL_TAG_NAME].excludeTags[tagCategory];
        }

        private Tag GetCurrTag() {
            return new Tag() {
                includeTags = IncludeTagContainer.GetTags(),
                excludeTags = ExcludeTagContainer.GetTags()
            };
        }

        private void GenerateHyperlink(object _0, RoutedEventArgs _1) {
            string address = SEARCH_ADDRESS + string.Join(
                ' ',
                CATEGORIES.Select((category, idx) => {
                    return IncludeTagContainer.GetSearchParameters(idx, GetGlobalTag(category, true)) + ' ' +
                    ExcludeTagContainer.GetSearchParameters(idx, GetGlobalTag(category, false));
                }).Where(searchParam => !string.IsNullOrWhiteSpace(searchParam))
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
                _mw.AlertUser("Please enter an ID(s) or an URL(s)", "");
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
                _mw.AlertUser("Invalid ID(s) or URL(s)", "Please enter valid ID(s) or URL(s)");
                return;
            }
            GalleryIDTextBox.Text = "";
            
            foreach (string extractedId in extractedIds) {
                // skip if:
                // it is already downloading OR
                
                if (DownloadingGalleries.Contains(extractedId)) {
                    continue;
                }
                // it is already bookmarked.
                if (GetBookmarkItem(extractedId) != null) {
                    continue;
                }
                // if the already loaded gallery is the same gallery just bookmark it
                if (_mw.gallery != null) {
                    if (extractedId == _mw.gallery.id) {
                        AddBookmark(_mw.gallery);
                        continue;
                    }
                }
                // Download
                DownloadingGalleries.Add(extractedId);
                DownloadPanel.Children.Add(new DownloadItem(extractedId, _mw.httpClient, this, DownloadPanel));
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
            if (_mw.gallery != null) {
                if (gallery.id == _mw.gallery.id) {
                    _mw.AlertUser("The gallery is already loaded", "");
                    return;
                }
            }
            _iwp.LoadGalleryFromLocalDir(gallery);
        }

        public static void WriteBookmark() {
            File.WriteAllText(BM_INFO_PATH, JsonSerializer.Serialize(bmItems.Select((bmItem) => { return bmItem.gallery; }), serializerOptions)); ;
        }

        public void AddBookmark(Gallery gallery) {
            lock (_bmLock) {
                // if it does not already exist in the bookmark
                if (GetBookmarkItem(gallery.id) == null) {
                    bmItems.Add(new BookmarkItem(gallery, this));

                    // new page is needed
                    if (bmItems.Count % MAX_BOOKMARK_PER_PAGE == 1) {
                        BookmarkPageSelector.Items.Add(BookmarkPageSelector.Items.Count);
                    }

                    WriteBookmark();
                    // if this is the first bookmark
                    if (bmItems.Count == 1) {
                        BookmarkPageSelector.SelectedIndex = 0;
                    } else {
                        FillBookmark();
                    }
                }
            }
        }

        public void RemoveBookmark(BookmarkItem bmItem) {
            lock (_bmLock) {
                // if a there is a loaded gallery and it is the gallery to be removed, call ChangeBookmarkBtnState, otherwise, just delete the gallery
                if (_mw.gallery != null && bmItem.gallery == _mw.gallery) {
                    _iwp.ChangeBookmarkBtnState(GalleryState.Loaded);
                } else {
                    DeleteGallery(bmItem.gallery);
                }
                bmItems.Remove(bmItem);
                WriteBookmark();

                bool pageChanged = false;
                // a page needs to be removed
                if (bmItems.Count % MAX_BOOKMARK_PER_PAGE == 0) {
                    // if current page is the last page
                    if (BookmarkPageSelector.SelectedIndex == BookmarkPageSelector.Items.Count - 1) {
                        pageChanged = true;
                        BookmarkPageSelector.SelectedIndex -= BookmarkPageSelector.SelectedIndex;
                    }
                    BookmarkPageSelector.Items.Remove(BookmarkPageSelector.Items.Count - 1);
                }

                // don't call FillBookmarkGrid again if page was changed because BookmarkPageSelector.SelectionChanged event would have called FillBookmarkGrid already
                if (!pageChanged) {
                    FillBookmark();
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
                WriteBookmark();
                FillBookmark();
            }
        }

        private void FillBookmark() {
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

        public static void ReloadBookmarkImages(int idx) {
            bmItems[idx].ReloadImages();
        }
    }
}
