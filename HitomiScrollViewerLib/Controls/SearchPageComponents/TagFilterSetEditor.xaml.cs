using HitomiScrollViewerLib.DbContexts;
using HitomiScrollViewerLib.Entities;
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
using static HitomiScrollViewerLib.Entities.TagFilterV3;
using static HitomiScrollViewerLib.SharedResources;
using static HitomiScrollViewerLib.Utils;

namespace HitomiScrollViewerLib.Controls.SearchPageComponents {
    public sealed partial class TagFilterSetEditor : Grid {
        private const string SEARCH_ADDRESS = "https://hitomi.la/search.html?";

        private static readonly ResourceMap _resourceMap = MainResourceMap.GetSubtree(typeof(TagFilterSetEditor).Name);

        private readonly TextBox[] _tagFilterTextBoxes = new TextBox[CATEGORIES.Length];
        private readonly ActionContentDialog _contentDialog = new();
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
                    Padding = new Thickness(0)
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
        }

        internal void Init() {
            ObservableCollection<TagFilterSet> collection = TagFilterSetContext.MainContext.TagFilterSets.Local.ToObservableCollection();
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

        internal void InsertTagFilters(ICollection<TagFilterV3> tagFilters) {
            foreach (TagFilterV3 tagFilter in tagFilters) {
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
                TagFilterSetContext.MainContext
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
                        category => new TagFilterV3() {
                            Category = category,
                            Tags = GetTags(category)
                        }
                    )
                    .ToList()
            };
            TagFilterSetContext.MainContext.TagFilterSets.Add(tagFilterSet);
            TagFilterSetContext.MainContext.SaveChanges();
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
            TagFilterSetContext.MainContext.TagFilterSets.First(tagFilterSet => tagFilterSet.Name == oldName).Name = newName;
            TagFilterSetContext.MainContext.SaveChanges();
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
                TagFilterSetContext.MainContext
                .TagFilterSets
                .First(tagFilterSet => tagFilterSet.Name == ((TagFilterSet)TagFilterSetComboBox.SelectedItem).Name);
            foreach (TagFilterV3 tagFilter in tagFilterSet.TagFilters) {
                tagFilter.Tags = GetTags(tagFilter.Category);
            }
            TagFilterSetContext.MainContext.SaveChanges();
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
            TagFilterSetContext.MainContext.TagFilterSets.Remove(TagFilterSetContext.MainContext.TagFilterSets.First(tagFilterSet => tagFilterSet.Name == name));
            TagFilterSetContext.MainContext.SaveChanges();
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
                MainWindow.CurrentMainWindow.NotifyUser(
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
                MainWindow.CurrentMainWindow.NotifyUser(
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
