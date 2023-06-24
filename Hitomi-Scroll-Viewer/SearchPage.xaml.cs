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
using System.Threading.Tasks;
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
        private static readonly string TAG_FILE_PATH = "Tag.json";

        private static readonly string GLOBAL_TAG_NAME = "Global";

        private static Dictionary<string, Tag> _tagDict;

        public static readonly double THUMBNAIL_IMG_WIDTH = 350;
        public static readonly int THUMBNAIL_IMG_NUM = 3;
        public static readonly int MAX_BOOKMARK_PER_PAGE = 3;
        public static readonly int MAX_BOOKMARK_PAGE = 5;

        private static readonly List<BookmarkItem> _bookmarkItems = new();
        private static int _currBookmarkPage = 0;

        private TagListControlButton _renameTagBtn;
        private TagListControlButton _removeTagBtn;

        private static readonly TagContainer[] _tagContainers = new TagContainer[2];

        private static readonly DataPackage _myDataPackage = new() {
            RequestedOperation = DataPackageOperation.Copy
        };

        private static MainWindow _mw;
        private static ImageWatchingPage _iwp;

        public SearchPage(MainWindow mainWindow) {
            InitializeComponent();
            InitLayout();
            _mw = mainWindow;

            //  if tag file doesn't exist create it and initialise with Global tag list
            if (!File.Exists(TAG_FILE_PATH)) {
                Tag globalTag = new();
                globalTag.includeTags["tag"] = new string[] { "non-h_imageset" };
                Dictionary<string, Tag> initTagDict = new() {
                    { GLOBAL_TAG_NAME, globalTag }
                };
                File.WriteAllText(TAG_FILE_PATH, JsonSerializer.Serialize(initTagDict, _serializerOptions));
            }
            // read tag info from file
            _tagDict = (Dictionary<string, Tag>)JsonSerializer.Deserialize(File.ReadAllText(TAG_FILE_PATH), typeof(Dictionary<string, Tag>), _serializerOptions);
            // add tags to TagListComboBox
            if (_tagDict.Count > 0) {
                foreach (KeyValuePair<string, Tag> item in _tagDict) {
                    TagListComboBox.Items.Add(item.Key);
                }
                TagListComboBox.SelectedIndex = 0;
            }

            // create bookmarked galleries' info file if it doesn't exist
            if (!File.Exists(BM_INFO_FILE_NAME)) {
                File.WriteAllText(BM_INFO_FILE_NAME, JsonSerializer.Serialize(new List<Gallery>(), _serializerOptions));
            }
            // read bookmarked galleries' info from file
            bmGalleries = (List<Gallery>)JsonSerializer.Deserialize(
                File.ReadAllText(BM_INFO_FILE_NAME),
                typeof(List<Gallery>),_serializerOptions);

            // create image storing directory if it doesn't exist
            if (!Directory.Exists(IMAGE_DIR)) {
                Directory.CreateDirectory(IMAGE_DIR);
            }

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
            TagListControlButton createTagBtn = new("Create a new tag list", Colors.Blue, false);
            createTagBtn.Click += CreateTag;
            ControlButtonContainer.Children.Add(createTagBtn);

            _renameTagBtn = new("Rename current tag list", Colors.Orange, false);
            _renameTagBtn.Click += RenameTag;
            ControlButtonContainer.Children.Add(_renameTagBtn);

            TagListControlButton saveTagBtn = new("Save current tag list", Colors.Green, true);
            saveTagBtn.SetAction(SaveTag);
            saveTagBtn.buttonClickFunc = () => {
                string selection = (string)TagListComboBox.SelectedItem;
                if (selection == null) {
                    _mw.AlertUser("No tag list selected", "");
                    return false;
                }
                saveTagBtn.SetDialog($"Save current tags on '{selection}'?", $"'{selection}' will be overwritten.");
                return true;
            };
            saveTagBtn.confirmDialog.Closed += (ContentDialog _, ContentDialogClosedEventArgs _) => {
                string selection = (string)TagListComboBox.SelectedItem;
                _mw.AlertUser($"'{selection}' was saved successfully.", "");
            };
            ControlButtonContainer.Children.Add(saveTagBtn);

            _removeTagBtn = new("Remove current tag list", Colors.Red, true);
            _removeTagBtn.SetAction(RemoveTag);
            _removeTagBtn.buttonClickFunc = () => {
                string selection = (string)TagListComboBox.SelectedItem;
                if (selection == null) {
                    _mw.AlertUser("No tag list selected", "");
                    return false;
                }
                _removeTagBtn.SetDialog($"Remove '{selection}'?", "");
                return true;
            };
            ControlButtonContainer.Children.Add(_removeTagBtn);

            TagListControlButton clearTagBtn = new("Clear current tags", Colors.Black, true);
            clearTagBtn.SetAction(ClearTagTextbox);
            clearTagBtn.buttonClickFunc = () => {
                clearTagBtn.SetDialog("Clear all tags in text box?", "");
                return true;
            };
            ControlButtonContainer.Children.Add(clearTagBtn);

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

        public static string[] GetGlobalTag(string tagCategory, bool isExclude) {
            if (isExclude) {
                return _tagDict[GLOBAL_TAG_NAME].excludeTags[tagCategory];
            }
            return _tagDict[GLOBAL_TAG_NAME].includeTags[tagCategory];
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
            ComboBox tagList = (ComboBox)sender;
            if (tagList.SelectedItem is not string tagName) {
                return;
            }
            if (tagName == GLOBAL_TAG_NAME) {
                _renameTagBtn.IsEnabled = false;
                _removeTagBtn.IsEnabled = false;
            } else {
                _renameTagBtn.IsEnabled = true;
                _removeTagBtn.IsEnabled = true;
            }
            Tag tag = _tagDict[tagName];
            _tagContainers[0].InsertTags(tag.includeTags);
            _tagContainers[1].InsertTags(tag.excludeTags);
        }

        private void CreateTag(object sender, RoutedEventArgs e) {
            string tagName = TagNameTextBox.Text.Trim();
            TagNameTextBox.Text = "";
            if (tagName.Length == 0) {
                _mw.AlertUser("No Tag Name", "Please enter a tag name");
                return;
            }
            foreach (string item in TagListComboBox.Items.Cast<string>()) {
                if (item == tagName) {
                    _mw.AlertUser("Duplicate Tag Name", "A tag list with the name already exists");
                    return;
                }
            }
            _tagDict.Add(tagName, GetCurrTag());
            TagListComboBox.Items.Add(tagName);
            TagListComboBox.SelectedItem = tagName;

            SaveTagInfo();
        }

        private void RenameTag(object sender, RoutedEventArgs e) {
            if (TagListComboBox.SelectedIndex == -1) {
                _mw.AlertUser("No Tags Selected", "There is no tag list selected currently.");
                return;
            }
            string tagName = TagNameTextBox.Text.Trim();
            TagNameTextBox.Text = "";
            if (tagName.Length == 0) {
                _mw.AlertUser("No Tag Name", "Please enter a tag name");
                return;
            }
            foreach (string item in TagListComboBox.Items.Cast<string>()) {
                if (item == tagName) {
                    _mw.AlertUser("Duplicate Tag Name", "A tag list with the name already exists");
                    return;
                }
            }
            string selectedItem = (string)TagListComboBox.SelectedItem;
            _tagDict.Add(tagName, _tagDict[selectedItem]);
            _tagDict.Remove(selectedItem);
            TagListComboBox.Items.Add(tagName);
            TagListComboBox.SelectedItem = tagName;
            TagListComboBox.Items.Remove(selectedItem);

            SaveTagInfo();
        }

        private void SaveTag(ContentDialog cd, ContentDialogButtonClickEventArgs e) {
            string selectedString = (string)TagListComboBox.SelectedItem;
            _tagDict[selectedString] = GetCurrTag();
            SaveTagInfo();
        }

        private void RemoveTag(ContentDialog cd, ContentDialogButtonClickEventArgs e) {
            string selectedItem = (string)TagListComboBox.SelectedItem;
            _tagDict.Remove(selectedItem);
            TagListComboBox.Items.Remove(selectedItem);
            TagListComboBox.SelectedIndex = 0;
            SaveTagInfo();
        }

        private static void ClearTagTextbox(ContentDialog cd, ContentDialogButtonClickEventArgs e) {
            _tagContainers[0].Clear();
            _tagContainers[1].Clear();
        }

        public static void SaveTagInfo() {
            File.WriteAllText(TAG_FILE_PATH, JsonSerializer.Serialize(_tagDict, _serializerOptions));
        }

        public static void SaveBookmarkInfo() {
            File.WriteAllText(BM_INFO_FILE_NAME, JsonSerializer.Serialize(bmGalleries, _serializerOptions));
        }

        // TODO convert grid to stackpanel
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
