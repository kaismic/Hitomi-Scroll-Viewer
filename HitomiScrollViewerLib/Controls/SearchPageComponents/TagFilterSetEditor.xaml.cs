using HitomiScrollViewerLib.DbContexts;
using HitomiScrollViewerLib.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.Windows.ApplicationModel.Resources;
using Soluling;
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
        public static TagFilterSetEditor Main { get; set; }

        private readonly TextBox[] _tagFilterTextBoxes = new TextBox[CATEGORIES.Length];
        private readonly CRUDActionContentDialog _crudActionContentDialog = new();
        internal Button HyperlinkCreateButton { get; set; }

        private readonly ProgressRing _comboBoxProgRing = new() {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        public TagFilterSetEditor() {
            InitializeComponent();

            for (int i = 0; i < TagFilterSetControlGrid.Children.Count; i++) {
                FrameworkElement elem = TagFilterSetControlGrid.Children[i] as FrameworkElement;
                SetColumn(elem, i);
                if (elem is Button button) {
                    button.IsEnabled = false;
                    button.Padding = new(16);
                }
            }
            SetColumn(_comboBoxProgRing, GetColumn(TagFilterSetComboBox));
            TagFilterSetControlGrid.Children.Add(_comboBoxProgRing);

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

            IncludeTFSSelector.RegisterPropertyChangedCallback(
                TFSSelector.AnyCheckedProperty,
                EnableHyperlinkCreateButton
            );
            ExcludeTFSSelector.RegisterPropertyChangedCallback(
                TFSSelector.AnyCheckedProperty,
                EnableHyperlinkCreateButton
            );

            Loaded += TagFilterSetEditor_Loaded;
        }

        private void ShowProgressRings(bool show) {
            if (show) {
                TagFilterSetComboBox.Visibility = Visibility.Collapsed;
                IncludeTFSSelector.Visibility = Visibility.Collapsed;
                ExcludeTFSSelector.Visibility = Visibility.Collapsed;
                _comboBoxProgRing.Visibility = Visibility.Visible;
                IncludeTFSSelectorProgRing.Visibility = Visibility.Visible;
                ExcludeTFSSelectorProgRing.Visibility = Visibility.Visible;
            } else {
                _comboBoxProgRing.Visibility = Visibility.Collapsed;
                IncludeTFSSelectorProgRing.Visibility = Visibility.Collapsed;
                ExcludeTFSSelectorProgRing.Visibility = Visibility.Collapsed;
                TagFilterSetComboBox.Visibility = Visibility.Visible; 
                IncludeTFSSelector.Visibility = Visibility.Visible;
                ExcludeTFSSelector.Visibility = Visibility.Visible;
            }
        }

        internal void Init() {
            ShowProgressRings(true);
            TagFilterSetComboBox.SelectedIndex = -1;
            ObservableCollection<TagFilterSet> collection = TagFilterSetContext.Main.TagFilterSets.Local.ToObservableCollection();
            TagFilterSetComboBox.ItemsSource = null;
            TagFilterSetComboBox.ItemsSource = collection;
            IncludeTFSSelector.SetCollectionSource(collection);
            ExcludeTFSSelector.SetCollectionSource(collection);
            _crudActionContentDialog.DeletingTFSSelector.SetCollectionSource(collection);

            CreateButton.IsEnabled = true;
            DeleteButton.IsEnabled = true;
            ShowProgressRings(false);
        }

        private void EnableHyperlinkCreateButton(DependencyObject _0, DependencyProperty _1) {
            HyperlinkCreateButton.IsEnabled = IncludeTFSSelector.AnyChecked || ExcludeTFSSelector.AnyChecked;
        }

        private void TagFilterSetEditor_Loaded(object sender, RoutedEventArgs e) {
            _crudActionContentDialog.XamlRoot = MainWindow.SearchPage.XamlRoot;
            Loaded -= TagFilterSetEditor_Loaded;
        }

        internal void InsertTagFilters(ICollection<TagFilterV3> tagFilters) {
            foreach (TagFilterV3 tagFilter in tagFilters) {
                _tagFilterTextBoxes[CATEGORY_INDEX_MAP[tagFilter.Category]].Text = string.Join(Environment.NewLine, tagFilter.Tags);
            }
        }

        private List<string> GetTags(string category) {
            return
                _tagFilterTextBoxes[CATEGORY_INDEX_MAP[category]]
                .Text
                .Split(NEW_LINE_SEPS, DEFAULT_STR_SPLIT_OPTIONS)
                .Distinct()
                .ToList();
        }

        private void ClearTextBoxes() {
            foreach (var textBox in _tagFilterTextBoxes) {
                textBox.Text = "";
            }
        }

        private void TagFilterSetComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (TagFilterSetComboBox.SelectedIndex == -1) {
                RenameButton.IsEnabled = SaveButton.IsEnabled = false;
                ClearTextBoxes();
                return;
            }
            RenameButton.IsEnabled = SaveButton.IsEnabled = true;
            InsertTagFilters(
                TagFilterSetContext.Main
                    .TagFilterSets
                    .Where(tfs => tfs.Id == ((TagFilterSet)TagFilterSetComboBox.SelectedItem).Id)
                    .Include(tfs => tfs.TagFilters)
                    .First()
                    .TagFilters
            );
        }

        private async void CreateButton_Click(object _0, RoutedEventArgs _1) {
            _crudActionContentDialog.SetDialogAction(CRUDActionContentDialog.Action.Create);
            if (await _crudActionContentDialog.ShowAsync() != ContentDialogResult.Primary) {
                return;
            }
            string name = _crudActionContentDialog.GetInputText();
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
            TagFilterSetContext.Main.TagFilterSets.Add(tagFilterSet);
            TagFilterSetContext.Main.SaveChanges();
            TagFilterSetComboBox.SelectedItem = tagFilterSet;
            MainWindow.SearchPage.ShowInfoBar(
                string.Format(
                    _resourceMap.GetValue("InfoBar_Message_Create_Complete").ValueAsString,
                    name
                )
            );
        }

        private async void RenameButton_Click(object _0, RoutedEventArgs _1) {
            TagFilterSet selectedTFS = (TagFilterSet)TagFilterSetComboBox.SelectedItem;
            string oldName = selectedTFS.Name;
            _crudActionContentDialog.SetDialogAction(CRUDActionContentDialog.Action.Rename, oldName);
            if (await _crudActionContentDialog.ShowAsync() != ContentDialogResult.Primary) {
                return;
            }
            string newName = _crudActionContentDialog.GetInputText();
            selectedTFS.Name = newName;
            TagFilterSetContext.Main.SaveChanges();
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
                TagFilterSetContext.Main
                .TagFilterSets
                .First(tagFilterSet => tagFilterSet.Id == ((TagFilterSet)TagFilterSetComboBox.SelectedItem).Id);
            foreach (TagFilterV3 tagFilter in tagFilterSet.TagFilters) {
                tagFilter.Tags = GetTags(tagFilter.Category);
            }
            TagFilterSetContext.Main.SaveChanges();
            MainWindow.SearchPage.ShowInfoBar(
                string.Format(
                    _resourceMap.GetValue("InfoBar_Message_Save_Complete").ValueAsString,
                    tagFilterSet.Name
                )
            );
        }

        // Special exception case for delete action: Handle delete action in _crudActionContentDialog PrimaryButtonClick event
        private async void DeleteButton_Click(object _0, RoutedEventArgs _1) {
            _crudActionContentDialog.SetDialogAction(CRUDActionContentDialog.Action.Delete);
            if (await _crudActionContentDialog.ShowAsync() != ContentDialogResult.Primary) {
                return;
            }
            MainWindow.SearchPage.ShowInfoBar(
                MultiPattern.Format(
                    _resourceMap.GetValue("InfoBar_Message_Delete_Complete").ValueAsString,
                    _crudActionContentDialog.DeletionCount
                )
            );
        }

        internal SearchLinkItem GetSearchLinkItem(ObservableCollection<SearchLinkItem> searchLinkItems) {
            TagFilterSet selectedTFS = (TagFilterSet)TagFilterSetComboBox.SelectedItem;

            IEnumerable<TagFilterSet> includeTFSs = IncludeTFSSelector.GetCheckedTagFilterSets();
            IEnumerable<TagFilterSet> excludeTFSs = ExcludeTFSSelector.GetCheckedTagFilterSets();
            IEnumerable<long> includeTFSIds = includeTFSs.Select(tfs => tfs.Id);
            IEnumerable<long> excludeTFSIds = excludeTFSs.Select(tfs => tfs.Id);
            TagFilterSetContext.Main.TagFilterSets
                .Where(tfs => includeTFSIds.Contains(tfs.Id) || excludeTFSIds.Contains(tfs.Id))
                .Include(tfs => tfs.TagFilters)
                .Load();
            IEnumerable<string>[] includeTagFilters = CATEGORIES.Select(category => Enumerable.Empty<string>()).ToArray();
            IEnumerable<string>[] excludeTagFilters = CATEGORIES.Select(category => Enumerable.Empty<string>()).ToArray();

            KeyValuePair<string, IEnumerable<string>>[] dupTagFiltersDict = new KeyValuePair<string, IEnumerable<string>>[CATEGORIES.Length];
            IEnumerable<string>[] currTagFiltersInTextBox = CATEGORIES.Select(GetTags).ToArray();

            void TakeTagFilterSetsUnion(IEnumerable<TagFilterSet> tagFilterSets, IEnumerable<string>[] tagFilters) {
                if (selectedTFS == null) {
                    foreach (TagFilterSet tagFilterSet in tagFilterSets) {
                        for (int i = 0; i < CATEGORIES.Length; i++) {
                            tagFilters[i] = tagFilters[i].Union(tagFilterSet.TagFilters.Select(tagFilter => tagFilter.Tags).ToArray()[i]);
                        }
                    }
                } else {
                    foreach (TagFilterSet tagFilterSet in tagFilterSets) {
                        IEnumerable<string>[] tagsArray =
                            tagFilterSet.Id == selectedTFS?.Id
                            ? currTagFiltersInTextBox // use the current tags in the TagFilterEditControl textboxes instead
                            : tagFilterSet.TagFilters.Select(tagFilter => tagFilter.Tags).ToArray();
                        for (int i = 0; i < CATEGORIES.Length; i++) {
                            tagFilters[i] = tagFilters[i].Union(tagsArray[i]);
                        }
                    }
                }
            }
            TakeTagFilterSetsUnion(includeTFSs, includeTagFilters);
            TakeTagFilterSetsUnion(excludeTFSs, excludeTagFilters);

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
                    _resourceMap.GetValue("Notification_Selected_TagFilterSets_Empty_Title").ValueAsString,
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
