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
using static HitomiScrollViewerLib.SharedResources;

namespace HitomiScrollViewerLib.Controls.SearchPageComponents {
    public sealed partial class TagFilterSetEditor : Grid {
        private const string SEARCH_ADDRESS = "https://hitomi.la/search.html?";
        private static readonly Dictionary<Category, string> CATEGORY_SEARCH_PARAM_DICT = new() {
            { Category.Tag, "tag" },
            { Category.Male, "male" },
            { Category.Female, "female" },
            { Category.Artist, "artist" },
            { Category.Group, "group" },
            { Category.Character, "character" },
            { Category.Series, "series" }
        };

        private static readonly ResourceMap _resourceMap = MainResourceMap.GetSubtree(typeof(TagFilterSetEditor).Name);
        public static TagFilterSetEditor Main { get; set; }

        private readonly TagTokenizingTextBox[] _tfsTextBoxes = new TagTokenizingTextBox[Entities.Tag.CATEGORY_NUM];

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

            for (int i = 0; i < Entities.Tag.CATEGORY_NUM; i++) {
                TextBoxesGrid.ColumnDefinitions.Add(new ColumnDefinition());
            }

            for (int i = 0; i < Entities.Tag.CATEGORY_NUM; i++) {
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
                    Text = Enum.GetName((Category)i)
                };
                categoryHeaderBorder.Child = categoryHeader;

                _tfsTextBoxes[i] = new() {
                    BorderBrush = new SolidColorBrush(Colors.Black),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(0),
                    Padding = new Thickness(0),
                    IsEnabled = false
                };
                SetRow(_tfsTextBoxes[i], 1);
                SetColumn(_tfsTextBoxes[i], i);
                TextBoxesGrid.Children.Add(_tfsTextBoxes[i]);
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

        private void TagFilterSetEditor_Loaded(object sender, RoutedEventArgs e) {
            _crudActionContentDialog.XamlRoot = MainWindow.SearchPage.XamlRoot;
            Loaded -= TagFilterSetEditor_Loaded;
        }

        internal void Init() {
            TagFilterSetComboBox.SelectedIndex = -1;
            ObservableCollection<TagFilterSet> collection = HitomiContext.Main.TagFilterSets.Local.ToObservableCollection();
            TagFilterSetComboBox.ItemsSource = null;
            TagFilterSetComboBox.ItemsSource = collection;
            IncludeTFSSelector.SetCollectionSource(collection);
            ExcludeTFSSelector.SetCollectionSource(collection);
            _crudActionContentDialog.DeletingTFSSelector.SetCollectionSource(collection);
            CreateButton.IsEnabled = true;
            DeleteButton.IsEnabled = true;
            for (int i = 0; i < Entities.Tag.CATEGORY_NUM; i++) {
                _tfsTextBoxes[i].IsEnabled = true;
            }
            _comboBoxProgRing.Visibility = Visibility.Collapsed;
            IncludeTFSSelectorProgRing.Visibility = Visibility.Collapsed;
            ExcludeTFSSelectorProgRing.Visibility = Visibility.Collapsed;
            TagFilterSetComboBox.Visibility = Visibility.Visible;
            IncludeTFSSelector.Visibility = Visibility.Visible;
            ExcludeTFSSelector.Visibility = Visibility.Visible;
        }

        private void EnableHyperlinkCreateButton(DependencyObject _0, DependencyProperty _1) {
            HyperlinkCreateButton.IsEnabled = IncludeTFSSelector.AnyChecked || ExcludeTFSSelector.AnyChecked;
        }

        private HashSet<Tag> GetCurrentTags() {
            return
                Enumerable
                .Range(0, Entities.Tag.CATEGORY_NUM)
                .Select(i => _tfsTextBoxes[i].SelectedTags)
                .SelectMany(tags => tags)
                .ToHashSet();
        }

        private void ClearTextBoxes() {
            foreach (var textBox in _tfsTextBoxes) {
                textBox.SelectedTags.Clear();
            }
        }

        private void TagFilterSetComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            ClearTextBoxes();
            if (TagFilterSetComboBox.SelectedIndex == -1) {
                RenameButton.IsEnabled = SaveButton.IsEnabled = false;
                return;
            }
            RenameButton.IsEnabled = SaveButton.IsEnabled = true;
            ICollection<Tag> selectedTFSTags = HitomiContext.Main
                .TagFilterSets
                .Include(tfs => tfs.Tags)
                .First(tfs => tfs.Id == ((TagFilterSet)TagFilterSetComboBox.SelectedItem).Id)
                .Tags;
            foreach (Tag tag in selectedTFSTags) {
                _tfsTextBoxes[(int)tag.Category].SelectedTags.Add(tag);
            }
        }

        private async void CreateButton_Click(object _0, RoutedEventArgs _1) {
            _crudActionContentDialog.SetDialogAction(CRUDActionContentDialog.Action.Create);
            if (await _crudActionContentDialog.ShowAsync() != ContentDialogResult.Primary) {
                return;
            }
            string name = _crudActionContentDialog.GetInputText();
            TagFilterSet tagFilterSet = new() {
                Name = name,
                Tags = GetCurrentTags()
            };
            HitomiContext.Main.TagFilterSets.Add(tagFilterSet);
            HitomiContext.Main.SaveChanges();
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
            HitomiContext.Main.SaveChanges();
            MainWindow.SearchPage.ShowInfoBar(
                string.Format(
                    _resourceMap.GetValue("InfoBar_Message_Rename_Complete").ValueAsString,
                    oldName,
                    newName
                )
            );
        }

        private void SaveButton_Click(object _0, RoutedEventArgs _1) {
            TagFilterSet tfs =
                HitomiContext.Main.TagFilterSets
                .First(tagFilterSet => tagFilterSet.Id == ((TagFilterSet)TagFilterSetComboBox.SelectedItem).Id);
            tfs.Tags = GetCurrentTags();
            HitomiContext.Main.SaveChanges();
            MainWindow.SearchPage.ShowInfoBar(
                string.Format(
                    _resourceMap.GetValue("InfoBar_Message_Save_Complete").ValueAsString,
                    tfs.Name
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
            IEnumerable<int> includeTFSIds = includeTFSs.Select(tfs => tfs.Id);
            IEnumerable<int> excludeTFSIds = excludeTFSs.Select(tfs => tfs.Id);
            HitomiContext.Main.TagFilterSets
                .Where(tfs => includeTFSIds.Contains(tfs.Id) || excludeTFSIds.Contains(tfs.Id))
                .Include(tfs => tfs.Tags)
                .Load();
            IEnumerable<Tag> includeTags = includeTFSs.SelectMany(tfs => tfs.Tags);
            IEnumerable<Tag> excludeTags = excludeTFSs.SelectMany(tfs => tfs.Tags);

            // check if all selected TFSs are all empty
            if (!includeTags.Any() && !excludeTags.Any()) {
                MainWindow.CurrentMainWindow.NotifyUser(
                    _resourceMap.GetValue("Notification_Selected_TagFilterSets_Empty_Title").ValueAsString,
                    ""
                );
                return null;
            }

            // update to use current tags in text boxes if a TFS is selected in combobox
            if (selectedTFS != null) {
                HashSet<Tag> currentTags = GetCurrentTags();
                if (includeTFSIds.Contains(selectedTFS.Id)) {
                    includeTags = includeTags.Except(selectedTFS.Tags).Union(currentTags);
                } else if (excludeTFSIds.Contains(selectedTFS.Id)) {
                    excludeTags = excludeTags.Except(selectedTFS.Tags).Union(currentTags);
                }
            }
            IEnumerable<Tag> dupTags = includeTags.Intersect(excludeTags);

            if (dupTags.Any()) {
                IEnumerable<string> dupTagStrs =
                    Enumerable
                    .Range(0, Entities.Tag.CATEGORY_NUM)
                    .Select(i => {
                        Category category = (Category)i;
                        string joinedValues = string.Join(", ", dupTags.Where(tag => tag.Category == category).Select(tag => tag.Value));
                        return category.ToString() + ": " + joinedValues;
                    });
                MainWindow.CurrentMainWindow.NotifyUser(
                    _resourceMap.GetValue("Notification_Duplicate_Tags_Title").ValueAsString,
                    string.Join(Environment.NewLine, dupTagStrs)
                );
                return null;
            }

            string searchLink = SEARCH_ADDRESS +
                string.Join(' ', includeTags.Select(tag => CATEGORY_SEARCH_PARAM_DICT[tag.Category] + ':' + tag.Value))
                + ' ' +
                string.Join(' ', excludeTags.Select(tag => '-' + CATEGORY_SEARCH_PARAM_DICT[tag.Category] + ':' + tag.Value))
                .Trim();

            string[] displayTexts = new string[Entities.Tag.CATEGORY_NUM];
            for (int i = 0; i < Entities.Tag.CATEGORY_NUM; i++) {
                string includeValues = string.Join(", ", includeTags.Where(tag => tag.Category == (Category)i).Select(tag => tag.Value));
                string excludeValues = string.Join(", ", excludeTags.Where(tag => tag.Category == (Category)i).Select(tag => '-' + tag.Value));
                displayTexts[i] = ((Category)i).ToString() + ": " + string.Join(", ", [includeValues, excludeValues]);
            }

            return new(
                searchLink,
                string.Join(Environment.NewLine, displayTexts),
                (_, arg) => searchLinkItems.Remove((SearchLinkItem)arg.Parameter)
            );
        }
    }
}
