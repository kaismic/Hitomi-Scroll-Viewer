using Hitomi_Scroll_Viewer.DbContexts;
using Hitomi_Scroll_Viewer.Entities;
using Hitomi_Scroll_Viewer.MainWindowComponent.SearchPageComponent.TagFilterSetEditorComponent;
using Microsoft.EntityFrameworkCore;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.Windows.ApplicationModel.Resources;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using static Hitomi_Scroll_Viewer.Entities.TagFilter;
using static Hitomi_Scroll_Viewer.Resources;
using static Hitomi_Scroll_Viewer.Utils;

namespace Hitomi_Scroll_Viewer.MainWindowComponent.SearchPageComponent {
    public sealed partial class TagFilterSetEditor : Grid {
        private const string SEARCH_ADDRESS = "https://hitomi.la/search.html?";

        private static readonly ResourceMap _resourceMap = MainResourceMap.GetSubtree("TagFilterSetEditor");

        private static readonly TagFilterSetContext _tagFilterSetContext = new();
        private class IndexedTextBox : TextBox {
            internal int Index { get; set; }
        }
        private readonly IndexedTextBox[] _tagFilterTextBoxes = new IndexedTextBox[CATEGORIES.Length];
        private readonly ActionContentDialog _contentDialog = new(_tagFilterSetContext);
        internal Button HyperlinkCreateButton { get; set; }

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

        private void EnableHyperlinkCreateButton(DependencyObject _0, DependencyProperty _1) {
            HyperlinkCreateButton.IsEnabled = IncludeTagFilterSetSelector.AnyChecked || ExcludeTagFilterSetSelector.AnyChecked;
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

        private void TagFilterSetComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (TagFilterSetComboBox.SelectedIndex == -1) {
                RenameButton.IsEnabled = SaveButton.IsEnabled = DeleteButton.IsEnabled = false;
                return;
            }
            RenameButton.IsEnabled = SaveButton.IsEnabled = DeleteButton.IsEnabled = true;
            InsertTagFilters(
                _tagFilterSetContext
                .TagFilterSets
                .First(tagFilterSet => tagFilterSet.Name == ((TagFilterSet)TagFilterSetComboBox.SelectedItem).Name)
                .TagFilters
            );
        }

        private async void CreateButton_Click(object _0, RoutedEventArgs _1) {
            _contentDialog.SetDialogType(
                ActionContentDialog.Action.Create,
                _resourceMap.GetValue("ActionContentDialog_Title_Create").ValueAsString,
                _resourceMap.GetValue("Text_Create").ValueAsString
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
            MainWindow.SearchPage.ShowInfoBar(
                string.Format(
                    _resourceMap.GetValue("InfoBar_Message_Create_Complete").ValueAsString,
                    name
                )
            );
        }

        private async void RenameButton_Click(object _0, RoutedEventArgs _1) {
            string oldName = ((TagFilterSet)TagFilterSetComboBox.SelectedItem).Name;
            _contentDialog.SetDialogType(
                ActionContentDialog.Action.Rename,
                _resourceMap.GetValue("ActionContentDialog_Title_Rename").ValueAsString,
                _resourceMap.GetValue("Text_Rename").ValueAsString,
                oldName
            );
            ContentDialogResult cdr = await _contentDialog.ShowAsync();
            if (cdr != ContentDialogResult.Primary) {
                return;
            }
            string newName = _contentDialog.GetText();
            _tagFilterSetContext.TagFilterSets.First(tagFilterSet => tagFilterSet.Name == oldName).Name = newName;
            _tagFilterSetContext.SaveChanges();
            MainWindow.SearchPage.ShowInfoBar(
                string.Format(
                    _resourceMap.GetValue("InfoBar_Message_Rename_Complete").ValueAsString,
                    oldName,
                    newName
                )
            );
        }

        private void SaveButton_Click(object _0, RoutedEventArgs _1) {
            TagFilterSet tagFilterSet =
                _tagFilterSetContext
                .TagFilterSets
                .First(tagFilterSet => tagFilterSet.Name == ((TagFilterSet)TagFilterSetComboBox.SelectedItem).Name);
            foreach (TagFilter tagFilter in tagFilterSet.TagFilters) {
                tagFilter.Tags = GetTags(tagFilter.Category);
            }
            _tagFilterSetContext.SaveChanges();
            MainWindow.SearchPage.ShowInfoBar(
                string.Format(
                    _resourceMap.GetValue("InfoBar_Message_Save_Complete").ValueAsString,
                    tagFilterSet.Name
                )
            );
        }

        private async void DeleteButton_Click(object _0, RoutedEventArgs _1) {
            string name = ((TagFilterSet)TagFilterSetComboBox.SelectedItem).Name;
            _contentDialog.SetDialogType(
                ActionContentDialog.Action.Delete,
                string.Format(_resourceMap.GetValue("ActionContentDialog_Title_Delete").ValueAsString, name),
                _resourceMap.GetValue("Text_Delete").ValueAsString
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
            MainWindow.SearchPage.ShowInfoBar(
                string.Format(
                    _resourceMap.GetValue("InfoBar_Message_Delete_Complete").ValueAsString,
                    name
                )
            );
        }

        internal SearchLinkItem GetSearchLinkItem(ObservableCollection<SearchLinkItem> searchLinkItems) {
            string selectedTagFilterSetName = ((TagFilterSet)TagFilterSetComboBox.SelectedItem).Name;

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
                                    dupTagFiltersPair.Key[0].ToString().ToUpper() + dupTagFiltersPair.Key[1..] + ": " +
                                    string.Join(", ", dupTagFiltersPair.Value) +
                                    Environment.NewLine
                            )
                            : result

                );
            if (overlappingTagFiltersText.Length != 0) {
                MainWindow.NotifyUser(
                    _resourceMap.GetValue("Notification_Duplicate_Tags_Title").ValueAsString,
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
                    _resourceMap.GetValue("Notification_TagFilterSets_Empty_Title").ValueAsString,
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

            return new(
                searchLink,
                displayText,
                (_, arg) => searchLinkItems.Remove((SearchLinkItem)arg.Parameter)
            );
        }
    }
}
