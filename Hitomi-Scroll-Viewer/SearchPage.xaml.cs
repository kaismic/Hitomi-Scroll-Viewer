using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using Windows.ApplicationModel.DataTransfer;

namespace Hitomi_Scroll_Viewer {
    public sealed partial class SearchPage : Page {
        private readonly string _hitomiBaseSearchDomain = "https://hitomi.la/search.html?";
        private readonly string[] _tagTypes = { "language", "female", "male", "artist", "character", "group", "series", "type", "tag" };
        private readonly JsonSerializerOptions _serializerOptions = new() { IncludeFields = true, WriteIndented = true };
        
        private readonly string _bookmarkedGalleryInfoFileName = "Bookmarked Gallery Info.json";
        public readonly BookmarkedGalleryInfo bookmarkedGalleryInfo;

        private readonly string _tagFileName = "Tag.json";
        private readonly Dictionary<string, Tag> _tag;
        
        private readonly string _bookmarkedImagesFolderName = "Bookmarked Images";
        private readonly double _thumbnailImgWidth = 350;
        private readonly int _thumbnailNum = 3;

        private string _currGalleryID;

        private readonly TextBox[] _includeTagTextBoxes;
        private readonly TextBox[] _excludeTagTextBoxes;
        private readonly Grid[] _tagGrids = new Grid[2];
        private readonly DataPackage dataPackage = new() {
            RequestedOperation = DataPackageOperation.Copy
        };

        private readonly MainWindow _mainWindow;

        public SearchPage(MainWindow mainWindow) {
            InitializeComponent();
            _mainWindow = mainWindow;

            _includeTagTextBoxes = new TextBox[_tagTypes.Length];
            _excludeTagTextBoxes = new TextBox[_tagTypes.Length];

            _tagGrids[0] = IncludeTagGrid;
            _tagGrids[1] = ExcludeTagGrid;

            foreach (Grid grid in _tagGrids) {
                for (int j = 0; j < 3; j++) {
                    grid.RowDefinitions.Add(new RowDefinition());
                }
                for (int j = 0; j < _tagTypes.Length; j++) {
                    grid.ColumnDefinitions.Add(new ColumnDefinition());
                }
            }

            //< Border BorderBrush = "Black" BorderThickness = "1" Grid.Row = "0" Grid.Column = "0" Grid.ColumnSpan = "_tagTypes.Length" >
            //    < TextBlock HorizontalAlignment = "Center" VerticalAlignment = "Center" Text = "Include/Exclude" />
            //</ Border >

            for (int i = 0; i < _tagGrids.Length; i++) {
                Grid tagGrid = _tagGrids[i];
                Border headingBorder = new() {
                    BorderBrush = new SolidColorBrush(Colors.Black),
                    BorderThickness = new Thickness(1),
                };
                Grid.SetRow(headingBorder, 0);
                Grid.SetColumn(headingBorder, 0);
                Grid.SetColumnSpan(headingBorder, _tagTypes.Length);
                tagGrid.Children.Add(headingBorder);

                TextBlock headingTextBlock = new() {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    FontSize = 20,
                    FontWeight = new Windows.UI.Text.FontWeight(600),
                };
                if (i == 0) {
                    headingTextBlock.Text = "Include";
                    headingTextBlock.Foreground = new SolidColorBrush(Colors.Green);
                }
                else {
                    headingTextBlock.Text = "Exclude";
                    headingTextBlock.Foreground = new SolidColorBrush(Colors.Red);
                }

                headingBorder.Child = headingTextBlock;
            }

            for (int i = 0; i < _tagGrids.Length; i++) {
                Grid tagGrid = _tagGrids[i];
                for (int j = 0; j < _tagTypes.Length; j++) {
                    //< Border BorderBrush = "Black" BorderThickness = "1" Grid.Row = "1" Grid.Column = "i" >
                    //    < TextBlock HorizontalAlignment = "Center" VerticalAlignment = "Center" Text = "_tagTypes[i]" />
                    //</ Border >
                    Border tagHeadingBorder = new() {
                        BorderBrush = new SolidColorBrush(Colors.Black),
                        BorderThickness = new Thickness(1),
                    };
                    Grid.SetRow(tagHeadingBorder, 1);
                    Grid.SetColumn(tagHeadingBorder, j);
                    tagGrid.Children.Add(tagHeadingBorder);

                    TextBlock headingTextBlock = new() {
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        Text = _tagTypes[j][..1].ToUpperInvariant() + _tagTypes[j][1..]
                    };
                    tagHeadingBorder.Child = headingTextBlock;

                    //< TextBox BorderBrush = "Black" BorderThickness = "1" Grid.Row = "2" Grid.Column = "i" AcceptsReturn = "True" TextWrapping = "Wrap" Height = "200" CornerRadius = "0" ></ TextBox >
                    TextBox textBox = new() {
                        BorderBrush = new SolidColorBrush(Colors.Black),
                        BorderThickness = new Thickness(1),
                        AcceptsReturn = true,
                        TextWrapping = TextWrapping.Wrap,
                        CornerRadius = new CornerRadius(0),
                        Padding = new Thickness(0),
                        Height = 200
                    };
                    Grid.SetRow(textBox, 2);
                    Grid.SetColumn(textBox, j);
                    tagGrid.Children.Add(textBox);
                    if (i == 0) {
                        _includeTagTextBoxes[j] = textBox;
                    }
                    else {
                        _excludeTagTextBoxes[j] = textBox;
                    }
                }
            }

            if (!File.Exists(_tagFileName)) {
                Tag defaultTag = new();
                defaultTag.includeTagTypes["language"] = new string[] { "chinese" };
                defaultTag.includeTagTypes["tag"] = new string[] { "non-h_imageset" };
                File.WriteAllText(_tagFileName, JsonSerializer.Serialize(defaultTag, _serializerOptions));
            }
            _tag = (Dictionary<string, Tag>)JsonSerializer.Deserialize(File.ReadAllText(_tagFileName), typeof(Dictionary<string, Tag>), _serializerOptions);
            if (_tag.Count > 0) {
                foreach (KeyValuePair<string, Tag> item in _tag) {
                    TagListComboBox.Items.Add(item.Key);
                }
                TagListComboBox.SelectedIndex = 0;
            }
            
            if (!File.Exists(_bookmarkedGalleryInfoFileName)) {
                File.WriteAllText(_bookmarkedGalleryInfoFileName, JsonSerializer.Serialize(new BookmarkedGalleryInfo(), _serializerOptions));
            }
            bookmarkedGalleryInfo = (BookmarkedGalleryInfo)JsonSerializer.Deserialize(File.ReadAllText(_bookmarkedGalleryInfoFileName), typeof(BookmarkedGalleryInfo), _serializerOptions);

            Directory.CreateDirectory(_bookmarkedImagesFolderName);
        }

        public void Init() {
            // create bookmarks and load images
            for (int i = 0; i < bookmarkedGalleryInfo.ids.Count; i++) {
                AddBookmarkToGrid(i);
            }
        }

        private string GetAddress() {
            string param = "";
            string tagType;
            for (int i = 0; i < _tagTypes.Length; i++) {
                tagType = _tagTypes[i];
                foreach (string tag in _includeTagTextBoxes[i].Text.Split(new[] { Environment.NewLine, "\r" }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)) {
                    param += tagType + "%3A" + tag.Replace(' ', '_') + "%20";
                }
                foreach (string tag in _excludeTagTextBoxes[i].Text.Split(new[] { Environment.NewLine, "\r" }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)) {
                    param += "-" + tagType + "%3A" + tag.Replace(' ', '_') + "%20";
                }
            }
            return _hitomiBaseSearchDomain + param;
        }

        private string GetHyperlinkDisplayText() {
            string linkText = "";
            string tagTypeText;
            for (int i = 0; i < _tagTypes.Length; i++) {
                tagTypeText = _tagTypes[i] + ": ";
                linkText += tagTypeText;
                foreach (string tag in _includeTagTextBoxes[i].Text.Split(new[] { Environment.NewLine, "\r" }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)) {
                    linkText += tag + " ";
                }
                foreach (string tag in _excludeTagTextBoxes[i].Text.Split(new[] { Environment.NewLine, "\r" }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)) {
                    linkText += "-" + tag + " ";
                }
                if (linkText[^tagTypeText.Length..] == tagTypeText) {
                    linkText = linkText[..^tagTypeText.Length];
                } else {
                    linkText += "\n";
                }
            }
            return linkText;
        }

        private Tag GetCurrTag() {
            Tag currTag = new(_tagTypes);
            string tagType;
            string[] tagArray;
            for (int i = 0; i < _tagTypes.Length; i++) {
                tagType = _tagTypes[i];

                tagArray = _includeTagTextBoxes[i].Text.Split(new[] { Environment.NewLine, "\r" }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                for (int j = 0; j < tagArray.Length; j++) {
                    tagArray[j] = tagArray[j].Replace(' ', '_');
                }
                currTag.includeTagTypes[tagType] = tagArray;

                tagArray = _excludeTagTextBoxes[i].Text.Split(new[] { Environment.NewLine, "\r" }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                for (int j = 0; j < tagArray.Length; j++) {
                    tagArray[j] = tagArray[j].Replace(' ', '_');
                }
                currTag.excludeTagTypes[tagType] = tagArray;
            }
            return currTag;
        }

        private void LoadTagInTextBox(object sender, SelectionChangedEventArgs e) {
            ComboBox comboBox = sender as ComboBox;
            if (comboBox.SelectedIndex == -1) {
                return;
            }
            string selectedTagString = comboBox.SelectedItem as string;
            Tag selectedTag = _tag[selectedTagString];

            string text;
            string tagType;
            for (int i = 0; i < _tagTypes.Length; i++) {
                tagType = _tagTypes[i];
                text = "";
                foreach (string tag in selectedTag.includeTagTypes[tagType]) {
                    text += tag + Environment.NewLine;
                }
                if (text != "") {
                    text = text.TrimEnd();
                }
                _includeTagTextBoxes[i].Text = text;
                text = "";
                foreach (string tag in selectedTag.excludeTagTypes[tagType]) {
                    text += tag + Environment.NewLine;
                }
                if (text != "") {
                    text = text.TrimEnd();
                }
                _excludeTagTextBoxes[i].Text = text;
            }
        }

        private void CreateTag(object sender, RoutedEventArgs e) {
            string tagName = TagNameTextBox.Text.Trim();
            TagNameTextBox.Text = "";
            if (tagName.Length == 0) {
                _mainWindow.AlertUser("No Tag Name", "Please enter a tag Name");
                return;
            }
            foreach (string item in TagListComboBox.Items) {
                if (item == tagName) {
                    _mainWindow.AlertUser("Same Tag Name", "A tag with the same name already exists");
                    return;
                }
            }
            _tag.Add(tagName, GetCurrTag());
            TagListComboBox.Items.Add(tagName);
            TagListComboBox.SelectedItem = tagName;
        }

        private void RenameTag(object sender, RoutedEventArgs e) {
            if (TagListComboBox.SelectedIndex == -1) {
                _mainWindow.AlertUser("No Tags Selected", "There are no tags selected.");
                return;
            }
            string tagName = TagNameTextBox.Text.Trim();
            TagNameTextBox.Text = "";
            if (tagName.Length == 0) {
                _mainWindow.AlertUser("No Tag Name", "Please enter a tag Name");
                return;
            }
            foreach (string item in TagListComboBox.Items) {
                if (item == tagName) {
                    _mainWindow.AlertUser("Same Tag Name", "A tag with the same name already exists");
                    return;
                }
            }
            string selectedItem = TagListComboBox.SelectedItem as string;
            _tag.Add(tagName, _tag[selectedItem]);
            _tag.Remove(selectedItem);
            TagListComboBox.Items.Add(tagName);
            TagListComboBox.SelectedItem = tagName;
            TagListComboBox.Items.Remove(selectedItem);
        }

        private void SaveTag(object sender, RoutedEventArgs e) {
            if (TagListComboBox.SelectedIndex == -1) {
                _mainWindow.AlertUser("No Tags Selected", "There are no tags selected.");
                return;
            }
            string selectedString = TagListComboBox.SelectedItem as string;
            _tag[selectedString] = GetCurrTag();
            SaveTagConfirmationTextBlock.Text = '"' + selectedString + '"' + " was saved successfully.";
        }

        private void RemoveTag(object sender, RoutedEventArgs e) {
            if (TagListComboBox.SelectedIndex == -1) {
                _mainWindow.AlertUser("No Tags Selected", "There are no tags selected.");
                return;
            }
            string selectedItem = TagListComboBox.SelectedItem as string;
            _tag.Remove(selectedItem);
            TagListComboBox.Items.Remove(selectedItem);
        }

        private void ClearTagTextboxes(object sender, RoutedEventArgs e) {
            foreach (TextBox tb in _includeTagTextBoxes) {
                tb.Text = string.Empty;
            }
            foreach (TextBox tb in _excludeTagTextBoxes) {
                tb.Text = string.Empty;
            }
        }

        public void SaveInfoToFiles() {
            File.WriteAllText(_tagFileName, JsonSerializer.Serialize(_tag, _serializerOptions));
            File.WriteAllText(_bookmarkedGalleryInfoFileName, JsonSerializer.Serialize(bookmarkedGalleryInfo, _serializerOptions));
        }

        private void GenerateHyperlink(object sender, RoutedEventArgs e) {
            // copy link to clipboard
            dataPackage.SetText(GetAddress());
            Clipboard.SetContent(dataPackage);

            Grid gd = new();
            for (int i = 0; i < 12; i++) {
                gd.ColumnDefinitions.Add(new ColumnDefinition());
            }

            HyperlinkButton hb = new() {
                Content = new TextBlock() {
                    Text = GetHyperlinkDisplayText(),
                    TextWrapping = TextWrapping.WrapWholeWords,
                    FontSize = 10,
                },
                NavigateUri = new Uri(GetAddress()),
                HorizontalAlignment = HorizontalAlignment.Stretch,
            };

            Grid.SetColumn(hb, 0);
            Grid.SetColumnSpan(hb, 10);
            gd.Children.Add(hb);
            
            Button btn = new() {
                Content = new TextBlock() {
                    Text = "Remove",
                    TextWrapping = TextWrapping.WrapWholeWords,
                },
                FontSize = 12,
            };
            btn.Click += RemoveHyperlink;
            Grid.SetColumn(btn, 10);
            Grid.SetColumnSpan(btn, 2);
            gd.Children.Add(btn);
            
            GeneratedHyperlinks.Children.Add(gd);

        }

        private void RemoveHyperlink(object sender, RoutedEventArgs e) {
            Button btn = sender as Button;
            Grid parent = btn.Parent as Grid;
            GeneratedHyperlinks.Children.Remove(parent);
        }

        private void ShowImages() {
            string id = ExtractGalleryID();
            if (string.IsNullOrEmpty(id)) {
                _mainWindow.AlertUser("Invalid ID or URL", "Please enter a correct ID or URL");
                return;
            }
            GalleryIDTextBox.Text = "";
            LoadImages(id);
        }

        private void HandleGalleryIDTextBoxKeyDown(object sender, KeyRoutedEventArgs e) {
            if (e.Key == Windows.System.VirtualKey.Enter) {
                ShowImages();
            }
        }

        private void HandleLoadImageBtnClick(object sender, RoutedEventArgs e) {
            ShowImages();
        }

        private string ExtractGalleryID() {
            string text = GalleryIDTextBox.Text;
            for (int i = 0; i < text.Length; i++) {
                if (!char.IsDigit(text[i])) {
                    return Regex.Match(GalleryIDTextBox.Text, @"[-|/](\d+)\.html").Value[1..^5];
                }
            }
            return text;
        }

        private void LoadImages(string id) {
            _currGalleryID = id;
            _mainWindow.imageWatchingPage.BookmarkBtn.Label = "Loading Images...";
            _mainWindow.imageWatchingPage.BookmarkBtn.IsEnabled = false;
            _mainWindow.imageWatchingPage.ShowImages(id);
            _mainWindow.SwitchPage();
        }

        private void HandleBookmarkClick(object sender, RoutedEventArgs e) {
            HyperlinkButton btn = sender as HyperlinkButton;
            Grid parent = btn.Parent as Grid;
            LoadImages(bookmarkedGalleryInfo.ids[BookmarkGrid.Children.IndexOf(parent)]);
        }

        private async void AddBookmarkToGrid(int idx) {
            int rowSpan = 6;
            int columnSpan = 13;

            Grid gr = new();
            for (int i = 0; i < rowSpan; i++) {
                gr.RowDefinitions.Add(new RowDefinition());
            }
            for (int i = 0; i < columnSpan; i++) {
                gr.ColumnDefinitions.Add(new ColumnDefinition());
            }
            gr.BorderBrush = new SolidColorBrush(Colors.Black);
            gr.BorderThickness = new Thickness(1);

            HyperlinkButton hb = new() {
                Content = new TextBlock() {
                    Text = bookmarkedGalleryInfo.titles[idx] + "\n" + bookmarkedGalleryInfo.ids[idx],
                    TextWrapping = TextWrapping.WrapWholeWords,
                    FontSize = 24,
                },
            };
            hb.Click += HandleBookmarkClick;

            Grid.SetRow(hb, 0);
            Grid.SetRowSpan(hb, 1);
            Grid.SetColumn(hb, 0);
            Grid.SetColumnSpan(hb, columnSpan - 1);
            gr.Children.Add(hb);

            string imgStorageFolderPath = _bookmarkedImagesFolderName + @"\" + bookmarkedGalleryInfo.ids[idx];
            for (int i = 0; i < _thumbnailNum; i++) {
                BitmapImage bmpimg = await _mainWindow.imageWatchingPage.GetImage(await File.ReadAllBytesAsync(imgStorageFolderPath + @"\" + i.ToString()));
                Image img = new() {
                    Source = bmpimg,
                    Width = _thumbnailImgWidth,
                    Height = _thumbnailImgWidth * bookmarkedGalleryInfo.imgRatios[idx][i],
                };

                Grid.SetRow(img, 1);
                Grid.SetRowSpan(img, rowSpan - 1);
                Grid.SetColumn(img, i * (columnSpan - 1) / _thumbnailNum);
                Grid.SetColumnSpan(img, (columnSpan - 1) / _thumbnailNum);
                gr.Children.Add(img);

            }

            Button btn = new() {
                Content = new TextBlock() {
                    Text = "Remove",
                    TextWrapping = TextWrapping.WrapWholeWords,
                },
                FontSize = 18,
            };
            btn.Click += RemoveBookmark;
            Grid.SetRow(btn, 0);
            Grid.SetRowSpan(btn, rowSpan);
            Grid.SetColumn(btn, columnSpan - 1);
            Grid.SetColumnSpan(btn, 1);
            gr.Children.Add(btn);

            BookmarkGrid.Children.Add(gr);
        }

        public void AddGalleryToBookmark(object _, RoutedEventArgs e) {
            _mainWindow.imageWatchingPage.ChangeBookmarkBtnState(false);
            bookmarkedGalleryInfo.ids.Add(_currGalleryID);
            bookmarkedGalleryInfo.titles.Add(_mainWindow.imageWatchingPage.currGalleryInfo.title);
            int imgIdx;
            int imgTotalCount = _mainWindow.imageWatchingPage.currByteArrays.Length;
            
            double[] imgRatios = new double[_thumbnailNum]; 
            string imgStorageFolderPath = _bookmarkedImagesFolderName + @"\" + _currGalleryID;
            Directory.CreateDirectory(imgStorageFolderPath);

            for (int i = 0; i < _thumbnailNum; i++) {
                imgIdx = imgTotalCount * i / _thumbnailNum;
                imgRatios[i] = (double)_mainWindow.imageWatchingPage.imgHeights[imgIdx] / _mainWindow.imageWatchingPage.imgWidths[imgIdx];
                File.WriteAllBytesAsync(imgStorageFolderPath + @"\" + i.ToString(), _mainWindow.imageWatchingPage.currByteArrays[imgIdx]);
            }
            bookmarkedGalleryInfo.imgRatios.Add(imgRatios);

            AddBookmarkToGrid(bookmarkedGalleryInfo.ids.Count-1);
        }

        private void RemoveBookmark(object sender, RoutedEventArgs e) {
            Button btn = sender as Button;
            Grid parent = btn.Parent as Grid;
            int targetIdx = BookmarkGrid.Children.IndexOf(parent);
            Directory.Delete(_bookmarkedImagesFolderName + @"\" + bookmarkedGalleryInfo.ids[targetIdx], true);
            if (bookmarkedGalleryInfo.ids[targetIdx] == _currGalleryID) {
                _mainWindow.imageWatchingPage.ChangeBookmarkBtnState(true);
            }
            bookmarkedGalleryInfo.ids.RemoveAt(targetIdx);
            bookmarkedGalleryInfo.titles.RemoveAt(targetIdx);
            bookmarkedGalleryInfo.imgRatios.RemoveAt(targetIdx);
            BookmarkGrid.Children.Remove(parent);
        }
    }
}
