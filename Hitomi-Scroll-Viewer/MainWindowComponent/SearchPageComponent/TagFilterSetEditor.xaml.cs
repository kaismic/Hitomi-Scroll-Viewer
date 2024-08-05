using Hitomi_Scroll_Viewer.DbContexts;
using Hitomi_Scroll_Viewer.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using static Hitomi_Scroll_Viewer.Entities.TagFilter;
using static Hitomi_Scroll_Viewer.Utils;
using static Hitomi_Scroll_Viewer.Resources;
using System.Collections.ObjectModel;
using Hitomi_Scroll_Viewer.MainWindowComponent.SearchPageComponent.TagFilterSetEditorComponent;

namespace Hitomi_Scroll_Viewer.MainWindowComponent.SearchPageComponent {
    public sealed partial class TagFilterSetEditor : Grid {
        private static readonly string SEARCH_ADDRESS = "https://hitomi.la/search.html?";

        private static readonly TagFilterSetContext _tagFilterSetContext = new();
        private class IndexedTextBox : TextBox {
            internal int Index { get; set; }
        }
        private readonly IndexedTextBox[] _tagFilterTextBoxes = new IndexedTextBox[CATEGORIES.Length];
        private readonly ActionContentDialog _contentDialog = new(_tagFilterSetContext);
        internal Button HyperlinkCreateButton { get; set; }
        private readonly bool[] _textBoxesEmpty = new bool[CATEGORIES.Length];
        private bool _allTextBoxesEmpty = true;

        public TagFilterSetEditor() {
            InitializeComponent();

            for (int i = 0; i < TagFilterSetControlGrid.Children.Count; i++) {
                FrameworkElement elem = TagFilterSetControlGrid.Children[i] as FrameworkElement;
                SetColumn(elem, i);
                if (elem is Button button) {
                    button.Padding = new(16);
                }
            }

            for (int i = 0; i < CATEGORIES.Length; i++) {
                TextBoxesGrid.ColumnDefinitions.Add(new ColumnDefinition());
            }

            for (int i = 0; i < CATEGORIES.Length; i++) {
                Border categoryHeaderBorder = new() {
                    BorderBrush = new SolidColorBrush(Colors.Black),
                    BorderThickness = new Thickness(1),
                };
                SetRow(categoryHeaderBorder, 0);
                SetColumn(categoryHeaderBorder, i);
                TextBoxesGrid.Children.Add(categoryHeaderBorder);

                TextBlock categoryHeader = new() {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Text = char.ToUpper(CATEGORIES[i][0]) + CATEGORIES[i][1..]
                };
                categoryHeaderBorder.Child = categoryHeader;

                _tagFilterTextBoxes[i] = new() {
                    BorderBrush = new SolidColorBrush(Colors.Black),
                    BorderThickness = new Thickness(1),
                    AcceptsReturn = true,
                    TextWrapping = TextWrapping.Wrap,
                    CornerRadius = new CornerRadius(0),
                    Padding = new Thickness(0),
                    Index = i
                };
                SetRow(_tagFilterTextBoxes[i], 1);
                SetColumn(_tagFilterTextBoxes[i], i);
                TextBoxesGrid.Children.Add(_tagFilterTextBoxes[i]);
                _tagFilterTextBoxes[i].TextChanged += (object sender, TextChangedEventArgs e) => {
                    IndexedTextBox indexedTextBox = sender as IndexedTextBox;
                    if (indexedTextBox.Text.Length == 0) {
                        _textBoxesEmpty[indexedTextBox.Index] = true;
                        _allTextBoxesEmpty = _tagFilterTextBoxes.All(textBox => textBox.Text.Length == 0);
                    } else {
                        _allTextBoxesEmpty = false;
                    }
                    EnableHyperlinkCreateButton(null, null);
                };
            }

            IncludeTagFilterSetSelector.RegisterPropertyChangedCallback(
                TagFilterSetSelector.AnyCheckedProperty,
                EnableHyperlinkCreateButton
            );
            ExcludeTagFilterSetSelector.RegisterPropertyChangedCallback(
                TagFilterSetSelector.AnyCheckedProperty,
                EnableHyperlinkCreateButton
            );

            Loaded += TagFilterSetEditor_Loaded;

            //_tagFilterSetContext.Database.EnsureDeleted();
            _tagFilterSetContext.Database.EnsureCreated();
            _tagFilterSetContext.TagFilterSets.Load();

            ObservableCollection<TagFilterSet> collection = _tagFilterSetContext.TagFilterSets.Local.ToObservableCollection();
            TagFilterSetComboBox.ItemsSource = collection;
            IncludeTagFilterSetSelector.Init(collection);
            ExcludeTagFilterSetSelector.Init(collection);
        }

        // TODO correct logic
        /**
         * if 
         */
        private void EnableHyperlinkCreateButton(DependencyObject _0, DependencyProperty _1) {
            HyperlinkCreateButton.IsEnabled = !_allTextBoxesEmpty || IncludeTagFilterSetSelector.AnyChecked || ExcludeTagFilterSetSelector.AnyChecked;
        }

        private void TagFilterSetEditor_Loaded(object sender, RoutedEventArgs e) {
            _contentDialog.XamlRoot = MainWindow.SearchPage.XamlRoot;
            Loaded -= TagFilterSetEditor_Loaded;
        }

        internal void InsertTagFilters(ICollection<TagFilter> tagFilters) {
            foreach (TagFilter tagFilter in tagFilters) {
                _tagFilterTextBoxes[CATEGORY_INDEX_MAP[tagFilter.Category]].Text = string.Join(Environment.NewLine, tagFilter.Tags);
            }
        }

        internal List<string> GetTags(string category) {
            return
                _tagFilterTextBoxes[CATEGORY_INDEX_MAP[category]]
                .Text
                .Split(NEW_LINE_SEPS, DEFAULT_STR_SPLIT_OPTIONS)
                .Distinct()
                .ToList();
        }

        internal List<string> GetTags(int idx) {
            return
                _tagFilterTextBoxes[idx]
                .Text
                .Split(NEW_LINE_SEPS, DEFAULT_STR_SPLIT_OPTIONS)
                .Distinct()
                .ToList();
        }

        internal string GetSelectedTagFilterSetName() {
            return (string)TagFilterSetComboBox.SelectedValue;
        }

        private void TagFilterSetComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (TagFilterSetComboBox.SelectedIndex == -1) {
                RenameButton.IsEnabled = SaveButton.IsEnabled = DeleteButton.IsEnabled = false;
                return;
            }
            RenameButton.IsEnabled = SaveButton.IsEnabled = DeleteButton.IsEnabled = true;
            InsertTagFilters(
                _tagFilterSetContext
                .TagFilterSets
                .First(tagFilterSet => tagFilterSet.Name == (string)TagFilterSetComboBox.SelectedValue)
                .TagFilters
            );
        }

        private async void CreateButton_Click(object _0, RoutedEventArgs _1) {
            _contentDialog.SetDialogType(
                ActionContentDialog.Action.Create,
                "Enter a name for the new tag filter set:", // TODO
                "Create" // TODO
            );
            ContentDialogResult cdr = await _contentDialog.ShowAsync();
            if (cdr != ContentDialogResult.Primary) {
                return;
            }
            string name = _contentDialog.GetText();
            TagFilterSet tagFilterSet = new() {
                Name = name,
                TagFilters =
                    CATEGORIES
                    .Select(
                        category => new TagFilter() {
                            Category = category,
                            Tags = GetTags(category)
                        }
                    )
                    .ToList()
            };
            _tagFilterSetContext.TagFilterSets.Add(tagFilterSet);
            _tagFilterSetContext.SaveChanges();
            TagFilterSetComboBox.SelectedItem = tagFilterSet;
            MainWindow.SearchPage.ShowInfoBar("Success", $"\"{name}\" has been created."); // TODO
        }

        private async void RenameButton_Click(object _0, RoutedEventArgs _1) {
            string oldName = (string)TagFilterSetComboBox.SelectedValue;
            _contentDialog.SetDialogType(
                ActionContentDialog.Action.Rename,
                "Enter a new name for the current tag filter set:", // TODO
                "Rename", // TODO
                oldName
            );
            ContentDialogResult cdr = await _contentDialog.ShowAsync();
            if (cdr != ContentDialogResult.Primary) {
                return;
            }
            string newName = _contentDialog.GetText();
            _tagFilterSetContext.TagFilterSets.First(tagFilterSet => tagFilterSet.Name == oldName).Name = newName;
            _tagFilterSetContext.SaveChanges();
            MainWindow.SearchPage.ShowInfoBar("Success", $"\"{oldName}\" has been renamed to \"{newName}\"."); // TODO
        }

        private void SaveButton_Click(object _0, RoutedEventArgs _1) {
            TagFilterSet tagFilterSet =
                _tagFilterSetContext
                .TagFilterSets
                .First(tagFilterSet => tagFilterSet.Name == (string)TagFilterSetComboBox.SelectedValue);
            foreach (TagFilter tagFilter in tagFilterSet.TagFilters) {
                tagFilter.Tags = GetTags(tagFilter.Category);
            }
            _tagFilterSetContext.SaveChanges();
            MainWindow.SearchPage.ShowInfoBar("Success", $"\"{tagFilterSet.Name}\" has been saved."); // TODO
        }

        private async void DeleteButton_Click(object _0, RoutedEventArgs _1) {
            string name = (string)TagFilterSetComboBox.SelectedValue;
            _contentDialog.SetDialogType(
                ActionContentDialog.Action.Delete,
                $"Delete \"{name}\"?", // TODO
                "Delete" // TODO
            );
            ContentDialogResult cdr = await _contentDialog.ShowAsync();
            if (cdr != ContentDialogResult.Primary) {
                return;
            }
            _tagFilterSetContext.TagFilterSets.Remove(_tagFilterSetContext.TagFilterSets.First(tagFilterSet => tagFilterSet.Name == name));
            _tagFilterSetContext.SaveChanges();
            foreach (TextBox textBox in _tagFilterTextBoxes) {
                textBox.Text = "";
            }
            MainWindow.SearchPage.ShowInfoBar("Success", $"\"{name}\" has been deleted."); // TODO
        }

        internal SearchLinkItem GetSearchLinkItem(ObservableCollection<SearchLinkItem> searchLinkItems) {
            string selectedTagFilterSetName = (string)TagFilterSetComboBox.SelectedValue;

            IEnumerable<TagFilterSet> includeTagFilterSets = IncludeTagFilterSetSelector.GetCheckedTagFilterSets();
            IEnumerable<TagFilterSet> excludeTagFilterSets = ExcludeTagFilterSetSelector.GetCheckedTagFilterSets();
            IEnumerable<string>[] includeTagFilters = CATEGORIES.Select(category => Enumerable.Empty<string>()).ToArray();
            IEnumerable<string>[] excludeTagFilters = CATEGORIES.Select(category => Enumerable.Empty<string>()).ToArray();

            KeyValuePair<string, IEnumerable<string>>[] dupTagFiltersDict = new KeyValuePair<string, IEnumerable<string>>[CATEGORIES.Length];
            IEnumerable<string>[] currTagFiltersInTextBox = CATEGORIES.Select(GetTags).ToArray();
            using TagFilterSetContext context = new();

            void TakeTagFilterSetsUnion(IEnumerable<TagFilterSet> tagFilterSets, IEnumerable<string>[] tagFilters) {
                foreach (TagFilterSet tagFilterSet in tagFilterSets) {
                    IEnumerable<string>[] tagsArray =
                        tagFilterSet.Name == selectedTagFilterSetName
                        ? currTagFiltersInTextBox // use the current tags in the TagFilterEditControl textboxes instead
                        : tagFilterSet.TagFilters.Select(tagFilter => tagFilter.Tags).ToArray();
                    for (int i = 0; i < CATEGORIES.Length; i++) {
                        tagFilters[i] = tagFilters[i].Union(tagsArray[i]);
                    }
                }
            }
            TakeTagFilterSetsUnion(includeTagFilterSets, includeTagFilters);
            TakeTagFilterSetsUnion(excludeTagFilterSets, excludeTagFilters);

            for (int i = 0; i < CATEGORIES.Length; i++) {
                dupTagFiltersDict[i] = new(CATEGORIES[i], includeTagFilters[i].Intersect(excludeTagFilters[i]));
            }

            string overlappingTagFiltersText =
                dupTagFiltersDict.Aggregate(
                    "",
                    (result, dupTagFiltersPair) =>
                        dupTagFiltersPair.Value.Any()
                            ? (
                                result +=
                                    dupTagFiltersPair.Key + ": " +
                                    string.Join(", ", dupTagFiltersPair.Value) +
                                    Environment.NewLine
                            )
                            : result

                );
            if (overlappingTagFiltersText.Length != 0) {
                MainWindow.NotifyUser(
                    //_resourceMap.GetValue("Notification_TagFilter_Overlap_Title").ValueAsString, TODO
                    "There are overlapping tags.",
                    overlappingTagFiltersText
                );
                return null;
            }

            string searchParams = string.Join(
                ' ',
                CATEGORIES.Select(
                    (category, i) =>
                        (
                            string.Join(' ', includeTagFilters[i].Select(tag => category + ':' + tag.Replace(' ', '_')))
                            + ' ' +
                            string.Join(' ', excludeTagFilters[i].Select(tag => '-' + category + ':' + tag.Replace(' ', '_')))
                        ).Trim()
                ).Where(searchParam => searchParam.Length != 0)
            );
            if (searchParams.Length == 0) {
                MainWindow.NotifyUser(
                    //_resourceMap.GetValue("Notification_SeachLink_ContentEmpty_Title").ValueAsString, TODO
                    "All of the selected tag filter sets are empty",
                    ""
                );
                return null;
            }
            string searchLink = SEARCH_ADDRESS + searchParams;

            string displayText = string.Join(
                Environment.NewLine,
                CATEGORIES.Select(
                    (category, i) => {
                        string displayTextPart =
                            (
                                string.Join(' ', includeTagFilters[i].Select(tag => tag.Replace(' ', '_')))
                                + ' ' +
                                string.Join(' ', excludeTagFilters[i].Select(tag => '-' + tag.Replace(' ', '_')))
                            ).Trim();
                        if (displayTextPart.Length != 0) {
                            return char.ToUpper(category[0]) + category[1..] + ": " + displayTextPart;
                        } else {
                            return "";
                        }
                    }
                ).Where(displayTagTexts => displayTagTexts.Length != 0)
            );

            return new (
                searchLink,
                displayText,
                (_, arg) => searchLinkItems.Remove((SearchLinkItem)arg.Parameter)
            );
        }
    }
}
