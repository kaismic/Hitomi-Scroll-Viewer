using Hitomi_Scroll_Viewer.SearchPageComponent;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private static readonly int[] GALLERY_ID_LENGTH_RANGE = new int[] { 6, 7 };
        private static readonly JsonSerializerOptions _serializerOptions = new() { IncludeFields = true, WriteIndented = true };

        private static readonly string BM_INFO_FILE_NAME = "BookmarkInfo.json";
        private static readonly string TAG_FILE_PATH = "sample1.json";

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
            new SolidColorBrush(Colors.Blue),
            new SolidColorBrush(Colors.Orange),
            new SolidColorBrush(Colors.Green),
            new SolidColorBrush(Colors.Red),
            new SolidColorBrush(Colors.Black)
        };

        private static MainWindow _mw;
        private static ImageWatchingPage _iwp;

        public SearchPage(MainWindow mainWindow) {
            InitializeComponent();
            InitLayout();
            _mw = mainWindow;

            if (File.Exists(TAG_FILE_PATH)) {
                // read tag info from file
                Tags = (Dictionary<string, Tag>)JsonSerializer.Deserialize(File.ReadAllText(TAG_FILE_PATH), typeof(Dictionary<string, Tag>), _serializerOptions);
            } else {
                //  if tag file doesn't exist, initialise _tags with Global tag list
                Tags = new() {
                    { GLOBAL_TAG_NAME,
                        new() {
                            includeTags = new() {
                                {
                                    "tag", new string[] { "non-h_imageset" }
                                }
                            }
                        }
                    }
                };
                File.WriteAllText(TAG_FILE_PATH, JsonSerializer.Serialize(Tags, _serializerOptions));
            }

            //TagListComboBox.SelectedIndex = 0;

            // create bookmarked galleries' info file if it doesn't exist
            if (!File.Exists(BM_INFO_FILE_NAME)) {
                File.WriteAllText(BM_INFO_FILE_NAME, JsonSerializer.Serialize(new List<Gallery>(), _serializerOptions));
            }
            // read bookmarked galleries' info from file
            bmGalleries = (List<Gallery>)JsonSerializer.Deserialize(
                File.ReadAllText(BM_INFO_FILE_NAME),
                typeof(List<Gallery>),_serializerOptions);

            // create image storing directory if it doesn't exist
            Directory.CreateDirectory(IMAGE_DIR);

            for (int i = 0; i < bmGalleries.Count; i++) {
                _bookmarkItems.Add(new(bmGalleries[i], this));
            }
            FillBookmarkGrid();
        }

        public static void Init(ImageWatchingPage iwp) {
            _iwp = iwp;
        }

        private void InitLayout() {
            // tag containers
            _tagContainers[0] = new(false, "Include", Colors.Green);
            _tagContainers[1] = new(true, "Exclude", Colors.Red);
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
                    XamlRoot = XamlRoot
                };
            }
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
            File.WriteAllText(TAG_FILE_PATH, JsonSerializer.Serialize(Tags, _serializerOptions));
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
            KeyValuePair<string, Tag> newPair = new(newTagName, GetCurrTag());
            _ = Tags.Append(newPair);
            TagListComboBox.SelectedItem = newPair;
            SaveTagInfo();
            _mw.AlertUser($"'{newTagName}' has been created", "");
        }

        private async void RenameTag(object _0, RoutedEventArgs _1) {
            string newTagName = TagNameTextBox.Text.Trim();
            if (newTagName.Length == 0) {
                _mw.AlertUser("No Tag Name", "Please enter a tag name");
                return;
            }
            if (Tags.ContainsKey(newTagName)) {
                _mw.AlertUser("Duplicate Tag Name", "A tag list with the name already exists");
                return;
            }
            _confirmDialogs[(int)TagListAction.Rename].Title = $"Rename {TagListComboBox.DisplayMemberPath} to '{newTagName}'?";
            ContentDialogResult cdr = await _confirmDialogs[(int)TagListAction.Rename].ShowAsync();
            if (cdr != ContentDialogResult.Primary) {
                return;
            }
            TagNameTextBox.Text = "";
            string oldTagName = _currTagName;
            KeyValuePair<string, Tag> newPair = new(newTagName, (Tag)TagListComboBox.SelectedValue);
            _ = Tags.Append(newPair);
            Tags.Remove(oldTagName);
            TagListComboBox.SelectedItem = newPair;
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
            _confirmDialogs[(int)TagListAction.Remove].Title = $"Remove '{_currTagName}'?";
            ContentDialogResult cdr = await _confirmDialogs[(int)TagListAction.Remove].ShowAsync();
            if (cdr != ContentDialogResult.Primary) {
                return;
            }
            Tags.Remove(_currTagName);
            TagListComboBox.SelectedIndex = 0;
            SaveTagInfo();
            _mw.AlertUser($"'{_currTagName}' has been removed", "");
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

        public static string[] GetGlobalTag(string tagCategory, bool isExclude) {
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

        private void LoadTagsInTextBox(object sender, SelectionChangedEventArgs _) {
            _currTagName = ((ComboBox)sender).DisplayMemberPath;
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

        private void GenerateHyperlink(object sender, RoutedEventArgs e) {
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

        private void RemoveHyperlink(object sender, RoutedEventArgs e) {
            HyperlinkPanel.Children.Remove((Grid)((Button)sender).Parent);
        }

        private void HandleGalleryIDSubmitKeyDown(object sender, KeyRoutedEventArgs e) {
            if (e.Key == Windows.System.VirtualKey.Enter) {
                LoadGalleryFromId();
            }
        }

        private void HandleLoadImageBtnClick(object sender, RoutedEventArgs e) {
            LoadGalleryFromId();
        }

        private async void LoadGalleryFromId() {
            string id = ExtractGalleryId();
            if (string.IsNullOrEmpty(id)) {
                _mw.AlertUser("Invalid ID or URL", "Please enter a valid ID or URL");
                return;
            }
            GalleryIDTextBox.Text = "";

            // if gallery is already loaded
            if (gallery != null) {
                if (id == gallery.id) {
                    _mw.SwitchPage();
                    return;
                }
            }

            // if gallery is already bookmarked
            for (int i = 0; i < bmGalleries.Count; i++) {
                if (bmGalleries[i].id == id) {
                    await _iwp.LoadGalleryFromLocalDir(bmGalleries[i]);
                    return;
                }
            }
            await _iwp.LoadImagesFromWeb(id);
        }

        private string ExtractGalleryId() {
            string regex = @"\d{"+ GALLERY_ID_LENGTH_RANGE[0] + "," + GALLERY_ID_LENGTH_RANGE[1] + "}";
            MatchCollection matches = Regex.Matches(GalleryIDTextBox.Text, regex);
            if (matches.Count == 0) {
                return null;
            }
            return matches[^1].Value;
        }

        public async void HandleBookmarkClick(object sender, RoutedEventArgs e) {
            BookmarkItem bmItem = (BookmarkItem)((HyperlinkButton)sender).Parent;

            // if gallery is already loaded
            if (gallery != null) {
                if (bmItem.bmGallery == gallery) {
                    _mw.AlertUser("Gallery is already loaded", "");
                    return;
                }
            }
            await _iwp.LoadGalleryFromLocalDir(bmItem.bmGallery);
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
            File.WriteAllText(BM_INFO_FILE_NAME, JsonSerializer.Serialize(bmGalleries, _serializerOptions));
        }

        public void AddBookmark(object _, RoutedEventArgs e) {
            HandleBookmarking(false);

            bmGalleries.Add(gallery);
            _bookmarkItems.Add(new BookmarkItem(gallery, this));
            FillBookmarkGrid();
            SaveBookmarkInfo();

            HandleBookmarking(true);
        }

        public void RemoveBookmark(object sender, RoutedEventArgs e) {
            BookmarkItem bmItem = (BookmarkItem)((Button)sender).Parent;

            // if the removing gallery is the current viewing gallery
            if (gallery != null) {
                if (bmItem.bmGallery == gallery) {
                    _iwp.ChangeBookmarkBtnState(GalleryState.Loaded);
                }
            }
            bmGalleries.Remove(bmItem.bmGallery);
            _bookmarkItems.Remove(bmItem);
            SaveBookmarkInfo();
            FillBookmarkGrid();
        }

        private void FillBookmarkGrid() {
            BookmarkPanel.Children.Clear();
            int startingIdx = _currBookmarkPage * MAX_BOOKMARK_PER_PAGE;
            int endingIdx = (_currBookmarkPage + 1) * MAX_BOOKMARK_PER_PAGE;

            for (int i = startingIdx; i < endingIdx; i++) {
                if (i < bmGalleries.Count) {
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
