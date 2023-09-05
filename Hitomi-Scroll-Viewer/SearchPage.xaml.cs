using Hitomi_Scroll_Viewer.SearchPageComponent;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using Windows.ApplicationModel.DataTransfer;
using static Hitomi_Scroll_Viewer.ImageWatchingPage;
using static Hitomi_Scroll_Viewer.MainWindow;
using static Hitomi_Scroll_Viewer.Tag;

namespace Hitomi_Scroll_Viewer {
    public sealed partial class SearchPage : Page {
        private static readonly string BASE_DOMAIN = "https://hitomi.la/search.html?";
        private static readonly Range GALLERY_ID_LENGTH_RANGE = 6..7;
        private static readonly JsonSerializerOptions _serializerOptions = new() { IncludeFields = true, WriteIndented = true };

        private static readonly string BM_INFO_FILE_PATH = ROOT_DIR + @"\BookmarkInfo.json";
        private static readonly string TAGS_FILE_PATH = ROOT_DIR + @"\Tags.json";

        private static readonly string GLOBAL_TAG_NAME = "Global";

        public static Dictionary<string, Tag> Tags { get; set; }

        public static readonly double THUMBNAIL_IMG_WIDTH = 350;
        public static readonly int THUMBNAIL_IMG_NUM = 3;
        public static readonly int MAX_BOOKMARK_PER_PAGE = 3;
        public static readonly int MAX_BOOKMARK_PAGE = 5;

        private static readonly List<BookmarkItem> _bookmarkItems = new();
        private static int _currBookmarkPage = 0;

        private static readonly TagContainer[] _tagContainers = new TagContainer[2];

        private static readonly DataPackage _myDataPackage = new() {
            RequestedOperation = DataPackageOperation.Copy
        };

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
                Tags = (Dictionary<string, Tag>)JsonSerializer.Deserialize(File.ReadAllText(TAGS_FILE_PATH), typeof(Dictionary<string, Tag>), _serializerOptions);
            } else {
                //  if tag file doesn't exist, initialise _tags with Global tag list
                Tags = new() {
                    { GLOBAL_TAG_NAME, new() }
                };
                Tags[GLOBAL_TAG_NAME].includeTags["tag"] = new string[] { "non-h_imageset" };
                File.WriteAllText(TAGS_FILE_PATH, JsonSerializer.Serialize(Tags, _serializerOptions));
            }

            foreach (KeyValuePair<string, Tag> item in Tags) {
                TagListComboBox.Items.Add(item.Key);
            }
            TagListComboBox.SelectedIndex = 0;

            // create bookmarked galleries' info file if it doesn't exist
            if (!File.Exists(BM_INFO_FILE_PATH)) {
                File.WriteAllText(BM_INFO_FILE_PATH, JsonSerializer.Serialize(new List<Gallery>(), _serializerOptions));
            }
            // read bookmarked galleries' info from file
            _mw.bmGalleries = (List<Gallery>)JsonSerializer.Deserialize(
                File.ReadAllText(BM_INFO_FILE_PATH),
                typeof(List<Gallery>),_serializerOptions
                );

            // fill bookmarks
            for (int i = 0; i < _mw.bmGalleries.Count; i++) {
                _bookmarkItems.Add(new(_mw.bmGalleries[i], this));
            }
            FillBookmarkGrid();
        }

        public void Init(ImageWatchingPage iwp) {
            _iwp = iwp;
            _iwp.BookmarkBtn.Click += AddBookmark;
        }

        private void InitLayout() {
            // tag containers
            _tagContainers[0] = new(this, false, "Include", Colors.Green);
            _tagContainers[1] = new(this, true, "Exclude", Colors.Red);
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
            void setXamlRoot(object s, RoutedEventArgs e) {
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

            // BookmarkPageBtns
            for (int i = 0; i < MAX_BOOKMARK_PAGE; i++) {
                Button pageNumBtn = new() {
                    Content = new TextBlock() {
                        Text = (i + 1).ToString(),
                        FontSize = 24,
                        Margin = new Thickness(6, 0, 6, 0)
                    },
                };
                pageNumBtn.Click += ChangeBookmarkPage;
                BookmarkPageBtnsPanel.Children.Add(pageNumBtn);
            }
        }

        public static void SaveTagInfo() {
            File.WriteAllText(TAGS_FILE_PATH, JsonSerializer.Serialize(Tags, _serializerOptions));
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
            SaveTagInfo();
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
            SaveTagInfo();
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
            SaveTagInfo();
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
            SaveTagInfo();
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
            return BASE_DOMAIN + param;
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
            removeBtn.Click += RemoveHyperlink;
            Grid.SetColumn(removeBtn, 1);
            gridItem.Children.Add(removeBtn);
            
            HyperlinkPanel.Children.Add(gridItem);
        }

        private void RemoveHyperlink(object sender, RoutedEventArgs _1) {
            HyperlinkPanel.Children.Remove((Grid)((Button)sender).Parent);
        }

        private void HandleKeyDown(object _0, KeyRoutedEventArgs e) {
            if (e.Key == Windows.System.VirtualKey.Enter) {
                CheckAndLoad();
            }
        }

        private void HandleLoadImageBtnClick(object _0, RoutedEventArgs _1) {
            CheckAndLoad();
        }

        public void EnableControls(bool enable) {
            LoadImageBtn.IsEnabled = enable;
            for (int i = 0; i < _bookmarkItems.Count; i++) {
                _bookmarkItems[i].EnableButton(enable);
            }
            if (enable) {
                GalleryIDTextBox.KeyDown += HandleKeyDown;
            } else {
                GalleryIDTextBox.KeyDown -= HandleKeyDown;
            }
        }

        private async void CheckAndLoad() {
            string id = ExtractGalleryId();
            if (string.IsNullOrEmpty(id)) {
                _mw.AlertUser("Invalid ID or URL", "Please enter a valid ID or URL");
                return;
            }
            GalleryIDTextBox.Text = "";

            // if gallery is already loaded
            if (_mw.gallery != null) {
                if (id == _mw.gallery.id && _mw.galleryState != GalleryState.Empty) {
                    _mw.SwitchPage();
                    return;
                }
            }

            // if gallery is already bookmarked
            for (int i = 0; i < _mw.bmGalleries.Count; i++) {
                if (_mw.bmGalleries[i].id == id) {
                    _mw.SwitchPage();
                    _iwp.LoadGalleryFromLocalDir(_mw.bmGalleries[i]);
                    return;
                }
            }
            _mw.SwitchPage();
            await _iwp.LoadGalleryFromWeb(id, 0);
        }

        private string ExtractGalleryId() {
            string regex = @"\d{" + GALLERY_ID_LENGTH_RANGE.Start + "," + GALLERY_ID_LENGTH_RANGE.End + "}";
            MatchCollection matches = Regex.Matches(GalleryIDTextBox.Text, regex);
            if (matches.Count == 0) {
                return null;
            }
            return matches[^1].Value;
        }

        public void HandleBookmarkClick(object sender, RoutedEventArgs _1) {
            BookmarkItem bmItem = (BookmarkItem)((HyperlinkButton)sender).Parent;

            // if gallery is already loaded
            if (_mw.gallery != null) {
                if (bmItem.gallery == _mw.gallery) {
                    _mw.AlertUser("Gallery is already loaded", "");
                    return;
                }
            }
            _mw.SwitchPage();
            _iwp.LoadGalleryFromLocalDir(bmItem.gallery);
        }

        /**
         * <summary>Call this method before and after bookmarking.</summary>
        */
        private void HandleBookmarking(bool isFinished) {
            if (isFinished) {
                _iwp.ChangeBookmarkBtnState(GalleryState.Bookmarked);
            } else {
                _iwp.ChangeBookmarkBtnState(GalleryState.Bookmarking);
            }
            GalleryIDTextBox.IsEnabled = isFinished;
            LoadImageBtn.IsEnabled = isFinished;

            // enable/disable bookmark page buttons
            foreach (Button btn in BookmarkPageBtnsPanel.Children.Cast<Button>()) {
                btn.IsEnabled = isFinished;
            }

            // enable/disable bookmark item hyperlink buttons
            foreach (BookmarkItem bmItem in BookmarkPanel.Children.Cast<BookmarkItem>()) {
                bmItem.EnableButton(isFinished);
            }
        }
        public static void SaveBookmarkInfo() {
            File.WriteAllText(BM_INFO_FILE_PATH, JsonSerializer.Serialize(_mw.bmGalleries, _serializerOptions));
        }

        public void AddBookmark(object _0, RoutedEventArgs _1) {
            HandleBookmarking(false);

            _mw.bmGalleries.Add(_mw.gallery);
            _bookmarkItems.Add(new BookmarkItem(_mw.gallery, this));
            SaveBookmarkInfo();
            FillBookmarkGrid();

            HandleBookmarking(true);
        }

        public void RemoveBookmark(object sender, RoutedEventArgs _1) {
            BookmarkItem bmItem = (BookmarkItem)((Grid)((Button)sender).Parent).Parent;

            // if the removing gallery is the current viewing gallery
            if (_mw.gallery != null) {
                if (bmItem.gallery == _mw.gallery) {
                    _iwp.ChangeBookmarkBtnState(GalleryState.Loaded);
                }
            }
            _mw.bmGalleries.Remove(bmItem.gallery);
            _bookmarkItems.Remove(bmItem);
            SaveBookmarkInfo();
            FillBookmarkGrid();
        }

        private void FillBookmarkGrid() {
            BookmarkPanel.Children.Clear();
            int startingIdx = _currBookmarkPage * MAX_BOOKMARK_PER_PAGE;
            int endingIdx = (_currBookmarkPage + 1) * MAX_BOOKMARK_PER_PAGE;

            for (int i = startingIdx; i < endingIdx; i++) {
                if (i < _mw.bmGalleries.Count) {
                    BookmarkPanel.Children.Add(_bookmarkItems[i]);
                }
            }
        }

        private void ChangeBookmarkPage(object sender, RoutedEventArgs e) {
            int btnIdx = BookmarkPageBtnsPanel.Children.IndexOf((Button)sender);
            if (_currBookmarkPage == btnIdx) {
                return;
            }
            _currBookmarkPage = btnIdx;
            FillBookmarkGrid();
        }
    }
}
