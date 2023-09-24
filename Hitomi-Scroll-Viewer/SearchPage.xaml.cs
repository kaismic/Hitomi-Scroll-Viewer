using Hitomi_Scroll_Viewer.SearchPageComponent;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using Windows.ApplicationModel.DataTransfer;
using static Hitomi_Scroll_Viewer.ImageWatchingPage;
using static Hitomi_Scroll_Viewer.Tag;
using static Hitomi_Scroll_Viewer.Utils;

namespace Hitomi_Scroll_Viewer {
    public sealed partial class SearchPage : Page {
        private static readonly string BM_INFO_FILE_PATH = Path.Combine(ROOT_DIR, "BookmarkInfo.json");
        private static readonly string TAGS_FILE_PATH = Path.Combine(ROOT_DIR, "Tags.json");

        private static readonly string SEARCH_ADDRESS = "https://hitomi.la/search.html?";
        private static readonly Range GALLERY_ID_LENGTH_RANGE = 6..7;

        private static readonly string GLOBAL_TAG_NAME = "Global";

        public static Dictionary<string, Tag> Tags { get; set; }

        public static readonly double THUMBNAIL_IMG_MAXHEIGHT = 256;
        public static readonly int THUMBNAIL_IMG_NUM = 3;
        public static readonly int MAX_BOOKMARK_PER_PAGE = 3;

        public static readonly List<BookmarkItem> bmItems = new();

        private static readonly TagContainer[] _tagContainers = new TagContainer[2];

        private static readonly DataPackage _myDataPackage = new() {
            RequestedOperation = DataPackageOperation.Copy
        };

        private readonly int[] _downloadThreadNums = new int[] { 1, 2, 3, 4, 5, 6, 7, 8 };
        public int DownloadThreadNum = 1;

        public readonly ConcurrentDictionary<string, byte> DownloadingGalleries = new();

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
        private readonly string[] _controlButtonTexts = new string[] {
            "Create a new tag list",
            "Rename current tag list",
            "Save current tag list",
            "Remove current tag list",
            "Clear texts in tag containers",
        };
        private readonly SolidColorBrush[] _controlButtonBorderColors = new SolidColorBrush[] {
            new(Colors.Blue),
            new(Colors.Orange),
            new(Colors.Green),
            new(Colors.Red),
            new(Colors.Black)
        };

        private static MainWindow _mw;
        private static ImageWatchingPage _iwp;

        public SearchPage(MainWindow mainWindow) {
            InitializeComponent();
            InitLayout();
            _mw = mainWindow;

            if (File.Exists(TAGS_FILE_PATH)) {
                // read tag info from file
                Tags = (Dictionary<string, Tag>)JsonSerializer.Deserialize(
                    File.ReadAllText(TAGS_FILE_PATH),
                    typeof(Dictionary<string, Tag>),
                    serializerOptions
                    );
            } else {
                //  if tag file doesn't exist, initialise _tags with Global tag list
                Tags = new() {
                    { GLOBAL_TAG_NAME, new() }
                };
                Tags[GLOBAL_TAG_NAME].includeTags["tag"] = new string[] { "non-h_imageset" };
                File.WriteAllText(TAGS_FILE_PATH, JsonSerializer.Serialize(Tags, serializerOptions));
            }

            foreach (KeyValuePair<string, Tag> item in Tags) {
                TagListComboBox.Items.Add(item.Key);
            }
            TagListComboBox.SelectedIndex = 0;

            // create bookmarked galleries' info file if it doesn't exist
            if (!File.Exists(BM_INFO_FILE_PATH)) {
                File.WriteAllText(BM_INFO_FILE_PATH, JsonSerializer.Serialize(new List<Gallery>(), serializerOptions));
            }
            // read bookmarked galleries' info from file
            _mw.bmGalleries = (List<Gallery>)JsonSerializer.Deserialize(
                File.ReadAllText(BM_INFO_FILE_PATH),
                typeof(List<Gallery>),
                serializerOptions
                );

            int pages = _mw.bmGalleries.Count / MAX_BOOKMARK_PER_PAGE + (_mw.bmGalleries.Count % MAX_BOOKMARK_PER_PAGE > 0 ? 1 : 0);
            for (int i = 0; i < pages; i++) {
                BookmarkPageSelector.Items.Add(i);
            }
            if (pages > 0) {
                BookmarkPageSelector.SelectedIndex = 0;
            }
            BookmarkPageSelector.SelectionChanged += (_, _) => {
                FillBookmarkGrid();
            };

            // fill bookmarks
            for (int i = 0; i < _mw.bmGalleries.Count; i++) {
                bmItems.Add(new(_mw.bmGalleries[i], this));
            }
            FillBookmarkGrid();
        }

        public static void Init(ImageWatchingPage iwp) {
            _iwp = iwp;
        }

        private void InitLayout() {
            // tag containers
            _tagContainers[0] = new(this, false);
            _tagContainers[1] = new(this, true);
            for (int i = 0; i < _tagContainers.Length; i++) {
                TagContainerGrid.ColumnDefinitions.Add(new ColumnDefinition());
                Grid.SetColumn(_tagContainers[i], i);
                TagContainerGrid.Children.Add(_tagContainers[i]);
            }

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
            File.WriteAllText(TAGS_FILE_PATH, JsonSerializer.Serialize(Tags, serializerOptions));
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
            _tagContainers[0].Clear();
            _tagContainers[1].Clear();
        }

        private void LoadTagsInTextBox(object sender, SelectionChangedEventArgs _1) {
            ComboBox cb = (ComboBox)sender;
            if (cb.SelectedItem == null) {
                cb.SelectedIndex = 0;
            }
            _currTagName = (string)cb.SelectedItem;
            // disable rename and remove button is global tag is selected
            if (_currTagName == GLOBAL_TAG_NAME) {
                _controlButtons[(int)TagListAction.Rename].IsEnabled = false;
                _controlButtons[(int)TagListAction.Remove].IsEnabled = false;
            }
            else {
                _controlButtons[(int)TagListAction.Rename].IsEnabled = true;
                _controlButtons[(int)TagListAction.Remove].IsEnabled = true;
            }
            Tag tag = Tags[_currTagName];
            _tagContainers[0].InsertTags(tag.includeTags);
            _tagContainers[1].InsertTags(tag.excludeTags);
        }

        public string[] GetGlobalTag(string tagCategory, bool isExclude) {
            if ((bool)IgnoreGlobalTagBtn.IsChecked) {
                return Array.Empty<string>();
            }
            if (isExclude) {
                return Tags[GLOBAL_TAG_NAME].excludeTags[tagCategory];
            }
            return Tags[GLOBAL_TAG_NAME].includeTags[tagCategory];
        }

        private static string GetSearchAddress() {
            string param = "";
            for (int i = 0; i < CATEGORIES.Length; i++) {
                param += _tagContainers[0].GetTagParameters(i);
                param += _tagContainers[1].GetTagParameters(i);
            }
            return SEARCH_ADDRESS + param;
        }

        private static string GetHyperlinkDisplayText() {
            string linkText = "";
            string tagTexts;

            for (int i = 0; i < CATEGORIES.Length; i++) {
                tagTexts = "";
                tagTexts += _tagContainers[0].GetTagStrings(i);
                tagTexts += _tagContainers[1].GetTagStrings(i);

                // if tag textbox is not empty
                if (tagTexts.Length > 0) {
                    linkText += char.ToUpper(CATEGORIES[i][0]) + CATEGORIES[i][1..] + ": " + tagTexts + Environment.NewLine;
                }
            }
            return linkText;
        }

        private static Tag GetCurrTag() {
            return new Tag() {
                includeTags = _tagContainers[0].GetTags(),
                excludeTags = _tagContainers[1].GetTags()
            };
        }

        private void GenerateHyperlink(object _0, RoutedEventArgs _1) {
            string address = GetSearchAddress();
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
                    Text = GetHyperlinkDisplayText(),
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
            MatchCollection ids = ExtractGalleryIds();
            if (ids.Count == 0) {
                _mw.AlertUser("Invalid ID(s) or URL(s)", "Please enter valid ID(s) or URL(s)");
                return;
            }
            GalleryIDTextBox.Text = "";

            for (int i = 0; i < ids.Count; i++) {
                // skip if:
                // it is already downloading OR
                if (DownloadingGalleries.TryGetValue(ids[i].Value, out _)) {
                    continue;
                }
                // it is already bookmarked.
                Gallery bmGallery = _mw.GetGalleryFromBookmark(ids[i].Value);
                if (bmGallery != null) {
                    continue;
                }
                // if the already loaded gallery is the same gallery just bookmark it
                if (_mw.gallery != null) {
                    if (ids[i].Value == _mw.gallery.id) {
                        AddBookmark(_mw.gallery);
                        continue;
                    }
                }
                // Download
                DownloadingGalleries.TryAdd(ids[i].Value, 0);
                DownloadPanel.Children.Add(new DownloadingItem(ids[i].Value, _mw.httpClient, this));
            }
        }

        public void EnableLoading(bool enable) {
            LoadGalleryBtn.IsEnabled = enable;
            for (int i = 0; i < bmItems.Count; i++) {
                bmItems[i].EnableHyperlinkButton(enable);
            }
        }

        private async void HandleLoadGalleryBtnClick(object _0, RoutedEventArgs _1) {
            string id = ExtractGalleryIds()[^1].Value;
            if (string.IsNullOrEmpty(id)) {
                _mw.AlertUser("Invalid ID or URL", "Please enter a valid ID or URL");
                return;
            }
            GalleryIDTextBox.Text = "";

            // id is valid so switch page
            _mw.SwitchPage();

            // if the already loaded gallery is the same gallery, just return
            if (_mw.gallery != null) {
                if (id == _mw.gallery.id) {
                    _mw.AlertUser("The gallery is already loaded", "");
                    return;
                }
            }
            // if it is already bookmarked, load it from local directory
            Gallery bmGallery = _mw.GetGalleryFromBookmark(id);
            if (bmGallery != null) {
                _iwp.LoadGalleryFromLocalDir(bmGallery);
                return;
            }
            await _iwp.LoadGalleryFromWeb(id);
        }

        private MatchCollection ExtractGalleryIds() {
            string regex = @"\d{" + GALLERY_ID_LENGTH_RANGE.Start + "," + GALLERY_ID_LENGTH_RANGE.End + "}";
            MatchCollection matches = Regex.Matches(GalleryIDTextBox.Text, regex);
            return matches;
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

        /**
         * <summary>Call this method before and after doing bookmark action/</summary>
        */
        public static void DoBookmarkAction(bool starting) {
            if (starting) {
                _mw.bmMutex.WaitOne();
            } else {
                _mw.bmMutex.ReleaseMutex();
            }
            for (int i = 0; i < bmItems.Count; i++) {
                bmItems[i].EnableHyperlinkButton(!starting);
            }
        }

        public static void WriteBookmark() {
            File.WriteAllText(BM_INFO_FILE_PATH, JsonSerializer.Serialize(_mw.bmGalleries, serializerOptions));
        }

        public void AddBookmark(Gallery gallery) {
            DoBookmarkAction(true);

            // if it does not already exist in the bookmark
            if (!_mw.bmGalleries.Contains(gallery)) {
                _mw.bmGalleries.Add(gallery);
                bmItems.Add(new BookmarkItem(gallery, this));

                // new page is needed
                if (_mw.bmGalleries.Count % MAX_BOOKMARK_PER_PAGE == 1) {
                    BookmarkPageSelector.Items.Add(BookmarkPageSelector.Items.Count);
                }

                WriteBookmark();
                // if this is the first bookmark
                if (bmItems.Count == 1) {
                    BookmarkPageSelector.SelectedIndex = 0;
                } else {
                    FillBookmarkGrid();
                }
            }

            DoBookmarkAction(false);
        }

        public void RemoveBookmark(object sender, RoutedEventArgs _1) {
            DoBookmarkAction(true);

            BookmarkItem bmItem = (BookmarkItem)((Button)sender).Parent;

            // delete bookmarked gallery if the removing gallery is not the current gallery
            if (_mw.gallery != null) {
                if (bmItem.gallery == _mw.gallery) {
                    _iwp.ChangeBookmarkBtnState(GalleryState.Loaded);
                } else {
                    DeleteGallery(bmItem.gallery);
                }
            } else {
                DeleteGallery(bmItem.gallery);
            }
            _mw.bmGalleries.Remove(bmItem.gallery);
            bmItems.Remove(bmItem);
            WriteBookmark();

            bool pageChanged = false;
            // a page needs to be removed
            if (_mw.bmGalleries.Count % MAX_BOOKMARK_PER_PAGE == 0) {
                // if current page is the last page
                if (BookmarkPageSelector.SelectedIndex == BookmarkPageSelector.Items.Count - 1) {
                    pageChanged = true;
                    BookmarkPageSelector.SelectedIndex -= BookmarkPageSelector.SelectedIndex;
                }
                BookmarkPageSelector.Items.Remove(BookmarkPageSelector.Items.Count - 1);
            }

            // don't call FillBookmarkGrid again if page was changed because of BookmarkPageSelector.SelectionChanged
            if (!pageChanged) {
                FillBookmarkGrid();
            }

            DoBookmarkAction(false);
        }

        private void FillBookmarkGrid() {
            BookmarkPanel.Children.Clear();
            int page = BookmarkPageSelector.SelectedIndex;
            if (page < 0) {
                return;
            }
            for (int i = page * MAX_BOOKMARK_PER_PAGE; i < (page + 1) * MAX_BOOKMARK_PER_PAGE; i++) {
                if (i < _mw.bmGalleries.Count) {
                    BookmarkPanel.Children.Add(bmItems[i]);
                }
            }
        }

        public bool IsBusy() {
            if (!DownloadingGalleries.IsEmpty) {
                _mw.AlertUser("Galleries are downloading", "Please cancel the downloading or wait for the downloading to finish before exiting.");
                return true;
            }
            return false;
        }
    }
}
