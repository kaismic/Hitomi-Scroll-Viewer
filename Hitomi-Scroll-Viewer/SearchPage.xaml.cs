using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Hitomi_Scroll_Viewer {
    public sealed partial class SearchPage : Page {
        private readonly string _tagFileName = "Tag.json";

        private readonly string _bookmarkedGalleryIdsFileName = "Bookmarked Gallery IDs.txt";
        public List<string> bookmarkedGalleryIdList = new();
        //private readonly string _bookmarkedImagesFolderName = "Bookmarked Images";
        private string _currGalleryID;

        private readonly JsonSerializerOptions _serializerOptions = new() { IncludeFields = true, WriteIndented = true };

        private readonly string _hitomiBaseSearchDomain = "https://hitomi.la/search.html?";
        private readonly string[] _tagTypes = { "language", "female", "male", "artist", "character", "series", "type", "tag" };
        private readonly Dictionary<string, Tag> _tag;

        private readonly List<TextBox> _includeTagTextBoxes = new();
        private readonly List<TextBox> _excludeTagTextBoxes = new();
        private readonly Grid[] _tagGrids = new Grid[2];

        private readonly MainWindow _mainWindow;

        public SearchPage(MainWindow mainWindow) {
            InitializeComponent();
            _mainWindow = mainWindow;

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
                        _includeTagTextBoxes.Add(textBox);
                    }
                    else {
                        _excludeTagTextBoxes.Add(textBox);
                    }
                }
            }

            if (!File.Exists(_tagFileName)) {
                string defaultTagList = """{"Default":{"includeTagTypes":{"language":[],"female":[],"male":[],"artist":[],"character":[],"series":[],"type":[],"tag":["non-h_imageset"]},"excludeTagTypes":{"language":[],"female":[],"male":[],"artist":[],"character":[],"series":[],"type":[],"tag":[]}}}""";
                File.WriteAllText(_tagFileName, defaultTagList);
            }
            _tag = (Dictionary<string, Tag>)JsonSerializer.Deserialize(File.ReadAllText(_tagFileName), typeof(Dictionary<string, Tag>), _serializerOptions);
            if (_tag.Count > 0) {
                foreach (KeyValuePair<string, Tag> item in _tag) {
                    TagListComboBox.Items.Add(item.Key);
                }
                TagListComboBox.SelectedIndex = 0;
            }

            if (!File.Exists(_bookmarkedGalleryIdsFileName)) {
                File.Create(_bookmarkedGalleryIdsFileName);
            }
            foreach (string id in File.ReadAllLines(_bookmarkedGalleryIdsFileName)) {
                bookmarkedGalleryIdList.Add(id);
            }

        }

        private string GetAddress() {
            string param = "";
            string tagType;
            for (int i = 0; i < _tagTypes.Length; i++) {
                tagType = _tagTypes[i];
                foreach (string tag in _includeTagTextBoxes[i].Text.Split(new[] { Environment.NewLine, "\r" }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)) {
                    if (tag.Length > 0) {
                        param += tagType + "%3A" + tag.Replace(' ', '_') + "%20";
                    }
                }
                foreach (string tag in _excludeTagTextBoxes[i].Text.Split(new[] { Environment.NewLine, "\r" }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)) {
                    if (tag.Length > 0) {
                        param += "-" + tagType + "%3A" + tag.Replace(' ', '_') + "%20";
                    }
                }
            }
            return _hitomiBaseSearchDomain + param;
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

        private void ClearTag(object sender, RoutedEventArgs e) {
            foreach (TextBox tb in _includeTagTextBoxes) {
                tb.Text = string.Empty;
            }
            foreach (TextBox tb in _excludeTagTextBoxes) {
                tb.Text = string.Empty;
            }
        }

        public void SaveTagToFile() {
            File.WriteAllText(_tagFileName, JsonSerializer.Serialize(_tag, _serializerOptions));
        }

        private void GenerateHyperlink(object sender, RoutedEventArgs e) {
            string address = GetAddress();

            HyperlinkButton hb = new() {
                Content = new TextBlock() {
                    Text = "Link " + (GeneratedHyperlinks.Children.Count + 1).ToString(),
                    TextWrapping = TextWrapping.WrapWholeWords,
                },
                NavigateUri = new Uri(address),
                HorizontalAlignment = HorizontalAlignment.Stretch,

            };
            Grid.SetColumn(hb, 0);
            Grid.SetColumnSpan(hb, 10);
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
            Grid gd = new();
            for (int i = 0; i < 12; i++) {
                gd.ColumnDefinitions.Add(new ColumnDefinition());
            }
            gd.Children.Add(hb);
            gd.Children.Add(btn);
            GeneratedHyperlinks.Children.Add(gd);

        }

        private void RemoveHyperlink(object sender, RoutedEventArgs e) {
            Button btn = sender as Button;
            Grid parent = btn.Parent as Grid;
            GeneratedHyperlinks.Children.Remove(parent);
        }

        private void HandleGalleryIDTextBoxKeyDown(object sender, KeyRoutedEventArgs e) {
            if (e.Key == Windows.System.VirtualKey.Enter) {
                LoadImage();
            }
        }
        private void HandleLoadImageBtnClick(object sender, RoutedEventArgs e) {
            LoadImage();
        }

        private void LoadImage() {
            string[] splitText = GalleryIDTextBox.Text.Split(".html");
            string idPart = splitText[0];
            string id = "";
            for (int i = idPart.Length - 1; i >= 0; i--) {
                if (!char.IsDigit(idPart[i])) {
                    id = idPart[(i + 1)..];
                }
            }
            if (string.IsNullOrEmpty(id)) {
                id = idPart;
            }
            _currGalleryID = id;
            _mainWindow.imageWatchingPage.ShowImages(id);
            _mainWindow.SwitchPage();
        }

        public void BookmarkGallery(object _, RoutedEventArgs e) {
            _mainWindow.imageWatchingPage.ChangeBookmarkBtnState(true);
            bookmarkedGalleryIdList.Add(_currGalleryID);

            int rowSpan = 6;
            int columnSpan = 13;
            int thumbnailNum = 3;
            double imgScale = 0.2;

            Grid gr = new();
            for (int i = 0; i < rowSpan; i++) {
                gr.RowDefinitions.Add(new RowDefinition());
            }
            for (int i = 0; i < columnSpan; i++) {
                gr.ColumnDefinitions.Add(new ColumnDefinition());
            }
            gr.BorderBrush = new SolidColorBrush(Colors.Black);
            gr.BorderThickness = new Thickness(1);


            TextBlock tb = new() {
                Text = _mainWindow.imageWatchingPage.currGalleryInfo.title + "\n" + _currGalleryID,
                TextWrapping = TextWrapping.WrapWholeWords,
                FontSize = 24
            };
            Grid.SetRow(tb, 0);
            Grid.SetRowSpan(tb, 1);
            Grid.SetColumn(tb, 0);
            Grid.SetColumnSpan(tb, columnSpan - 1);
            gr.Children.Add(tb);

            int imgTotalCount = _mainWindow.imageWatchingPage.currImages.Count;
            for (int i = 0; i < thumbnailNum; i++) {
                Image originImg = _mainWindow.imageWatchingPage.currImages[(imgTotalCount / thumbnailNum) * i];
                Image img = new() {
                    Source = originImg.Source,
                    Width = originImg.Width * imgScale,
                    Height = originImg.Height * imgScale
                };
                Grid.SetRow(img, 1);
                Grid.SetRowSpan(img, rowSpan - 1);
                Grid.SetColumn(img, i * (columnSpan - 1) / thumbnailNum);
                Grid.SetColumnSpan(img, (columnSpan - 1) / thumbnailNum);
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

        private void RemoveBookmark(object sender, RoutedEventArgs e) {
            Button btn = sender as Button;
            Grid parent = btn.Parent as Grid;
            string bookmarkedGalleryID = (parent.Children[0] as TextBlock).Text.Split('\n')[1];
            bookmarkedGalleryIdList.Remove(bookmarkedGalleryID);
            if (bookmarkedGalleryID == _currGalleryID) {
                _mainWindow.imageWatchingPage.ChangeBookmarkBtnState(false);
            }
            BookmarkGrid.Children.Remove(parent);
        }
    }
}
