using HitomiScrollViewerLib.DbContexts;
using HitomiScrollViewerLib.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.Windows.ApplicationModel.Resources;
using Soluling;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.Storage;
using static HitomiScrollViewerLib.SharedResources;

namespace HitomiScrollViewerLib.Controls.SearchPageComponents {
    public sealed partial class TagFilterSetEditor : Grid, IAppWindowClosingHandler {
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
        private static readonly string AUTO_SAVE_SETTING_KEY = "AutoSave";

        private static readonly ResourceMap _resourceMap = MainResourceMap.GetSubtree(typeof(TagFilterSetEditor).Name);
        public static TagFilterSetEditor Main { get; set; }

        private readonly TagTokenizingTextBox[] _tfsTextBoxes = new TagTokenizingTextBox[Entities.Tag.CATEGORY_NUM];

        private readonly CRUDActionContentDialog _crudActionContentDialog = new();
        internal Button HyperlinkCreateButton { get; set; }

        private readonly ProgressRing _comboBoxProgRing = new() {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        private readonly IEnumerable<GalleryType> _galleryTypes =
            GalleryType.DisplayNames
            .Select(
                displayName => new GalleryType() { DisplayName = displayName }
            );

        public TagFilterSetEditor() {
            InitializeComponent();

            for (int i = 0; i < Children.Count; i++) {
                SetRow(Children[i] as FrameworkElement, i);
            }

            AutoSaveCheckBox.Content = new TextBlock() {
                Text = _resourceMap.GetValue("AutoSaveCheckBox_Content").ValueAsString,
                TextWrapping = TextWrapping.WrapWholeWords
            };
            AutoSaveCheckBox.IsChecked = (bool)(ApplicationData.Current.LocalSettings.Values[AUTO_SAVE_SETTING_KEY] ?? true);
            void SaveAutoSaveSettingValue() {
                ApplicationData.Current.LocalSettings.Values[AUTO_SAVE_SETTING_KEY] = AutoSaveCheckBox.IsChecked;
            }
            AutoSaveCheckBox.Checked += (_, _) => SaveAutoSaveSettingValue();
            AutoSaveCheckBox.Unchecked += (_, _) => SaveAutoSaveSettingValue();

            for (int i = 0; i < TagFilterSetControlGrid.Children.Count; i++) {
                FrameworkElement elem = TagFilterSetControlGrid.Children[i] as FrameworkElement;
                SetColumn(elem, i);
                if (elem is Button button) {
                    button.SizeChanged += (object sender, SizeChangedEventArgs args) => {
                        button.Width = args.NewSize.Height;
                    };
                    button.IsEnabled = false;
                    button.VerticalAlignment = VerticalAlignment.Stretch;
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
                    Category = (Category)i,
                    BorderBrush = new SolidColorBrush(Colors.Black),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(0),
                    Padding = new Thickness(0),
                    IsEnabled = false
                };
                StackPanel tfsTextBoxWrapper = new();
                tfsTextBoxWrapper.Children.Add(_tfsTextBoxes[i]);
                SetRow(tfsTextBoxWrapper, 1);
                SetColumn(tfsTextBoxWrapper, i);
                TextBoxesGrid.Children.Add(tfsTextBoxWrapper);
            }

            IncludeTFSSelector.RegisterPropertyChangedCallback(
                TFSSelector.AnyCheckedProperty,
                (_, _) => EnableHyperlinkCreateButton()
            );
            ExcludeTFSSelector.RegisterPropertyChangedCallback(
                TFSSelector.AnyCheckedProperty,
                (_, _) => EnableHyperlinkCreateButton()
            );

            GLASBWrapper.AutoSuggestBox.TextChanged += (_, _) => EnableHyperlinkCreateButton();

            Loaded += TagFilterSetEditor_Loaded;
            SizeChanged += (object sender, SizeChangedEventArgs e) => {
                foreach (TagTokenizingTextBox tttb in _tfsTextBoxes) {
                    double maxHeight = TTTBsRowDef.ActualHeight - 16;
                    if (maxHeight >= 0) {
                        tttb.MaxHeight = maxHeight;
                    }
                }
            };
        }

        public void HandleAppWindowClosing(AppWindowClosingEventArgs args) {
            if (TagFilterSetComboBox.SelectedItem is TagFilterSet tfs) {
                SaveTFS(tfs);
            }
        }

        private void TagFilterSetEditor_Loaded(object sender, RoutedEventArgs e) {
            _crudActionContentDialog.XamlRoot = MainWindow.SearchPage.XamlRoot;
            Loaded -= TagFilterSetEditor_Loaded;
        }

        internal void Init() {
            CreateButton.IsEnabled = true;
            DeleteButton.IsEnabled = true;
            CreateButton.Focus(FocusState.Programmatic);
            ObservableCollection<TagFilterSet> collection = HitomiContext.Main.TagFilterSets.Local.ToObservableCollection();
            TagFilterSetComboBox.ItemsSource = null;
            TagFilterSetComboBox.ItemsSource = collection;
            IncludeTFSSelector.SetCollectionSource(collection);
            ExcludeTFSSelector.SetCollectionSource(collection);
            _crudActionContentDialog.DeletingTFSSelector.SetCollectionSource(collection);
            GLASBWrapper.AutoSuggestBox.IsEnabled = true;
            GalleryTypeComboBox.IsEnabled = true;
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

        private void EnableHyperlinkCreateButton() {
            HyperlinkCreateButton.IsEnabled =
                GalleryTypeComboBox.SelectedIndex > 0 ||
                GLASBWrapper.SelectedGL != null ||
                IncludeTFSSelector.AnyChecked || ExcludeTFSSelector.AnyChecked;
        }

        private void GalleryTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            EnableHyperlinkCreateButton();
        }

        private HashSet<Tag> GetCurrentTags() {
            return
                Enumerable
                .Range(0, Entities.Tag.CATEGORY_NUM)
                .Select(i => _tfsTextBoxes[i].SelectedTags)
                .SelectMany(tags => tags)
                .ToHashSet();
        }

        private void TagFilterSetComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (e.RemovedItems.Count > 0 && e.RemovedItems[0] is TagFilterSet prevTFS && AutoSaveCheckBox.IsChecked == true) {
                // do not save if this selection change occurred due to deletion of currently selected tfs
                if (_crudActionContentDialog.DeletedTFSIds == null) {
                    SaveTFS(prevTFS);
                } else if (!_crudActionContentDialog.DeletedTFSIds.Contains(prevTFS.Id)) {
                    SaveTFS(prevTFS);
                }
            }
            foreach (var textBox in _tfsTextBoxes) {
                textBox.SelectedTags.Clear();
            }
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

        private void SaveTFS(TagFilterSet tfs) {
            tfs.Tags = GetCurrentTags();
            HitomiContext.Main.SaveChanges();
            MainWindow.SearchPage.ShowInfoBar(
                string.Format(
                    _resourceMap.GetValue("InfoBar_Message_Save_Complete").ValueAsString,
                    tfs.Name
                )
            );
        }

        private void SaveButton_Click(object _0, RoutedEventArgs _1) {
            SaveTFS(
                HitomiContext.Main.TagFilterSets
                .First(tagFilterSet => tagFilterSet.Id == ((TagFilterSet)TagFilterSetComboBox.SelectedItem).Id)
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
                    _crudActionContentDialog.DeletedTFSIds.Count
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
            if (!includeTags.Any() && !excludeTags.Any() && GalleryTypeComboBox.SelectedIndex <= 0 && GLASBWrapper.SelectedGL == null) {
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
                List<string> dupTagStrs = new(Entities.Tag.CATEGORY_NUM);
                for (int i = 0; i < Entities.Tag.CATEGORY_NUM; i++) {
                    Category category = (Category)i;
                    IEnumerable<string> dupStrs = dupTags.Where(tag => tag.Category == category).Select(tag => tag.Value);
                    if (!dupStrs.Any()) {
                        continue;
                    }
                    dupTagStrs.Add(category.ToString() + ": " + string.Join(", ", dupStrs));
                }
                MainWindow.CurrentMainWindow.NotifyUser(
                    _resourceMap.GetValue("Notification_Duplicate_Tags_Title").ValueAsString,
                    string.Join(Environment.NewLine, dupTagStrs)
                );
                return null;
            }

            List<string> searchParamStrs = new(4); // 2 for joined include and exclude tags. 2 for language and type
            List<string> displayTexts = new(Entities.Tag.CATEGORY_NUM + 2); // + 2 for language and type

            if (GLASBWrapper.SelectedGL != null) {
                searchParamStrs.Add("language:" + GLASBWrapper.SelectedGL.SearchParamValue);
                displayTexts.Add("Language: " + GLASBWrapper.SelectedGL.DisplayName);
            }

            // only add type if !(nothing selected: -1 || "Any" type is selected: 0)
            if (GalleryTypeComboBox.SelectedIndex > 0) {
                GalleryType galleryType = GalleryTypeComboBox.SelectedItem as GalleryType;
                searchParamStrs.Add("type:" + galleryType.SearchParamValue);
                displayTexts.Add("Type: " + galleryType.DisplayName);
            }
            
            // add joined include exclude search param strs
            searchParamStrs.Add(string.Join(' ', includeTags.Select(tag => CATEGORY_SEARCH_PARAM_DICT[tag.Category] + ':' + tag.SearchParamValue)));
            searchParamStrs.Add(string.Join(' ', excludeTags.Select(tag => '-' + CATEGORY_SEARCH_PARAM_DICT[tag.Category] + ':' + tag.SearchParamValue)));

            // add display texts for each tag category
            for (int i = 0; i < Entities.Tag.CATEGORY_NUM; i++) {
                IEnumerable<string> includeValues = includeTags.Where(tag => tag.Category == (Category)i).Select(tag => tag.Value);
                IEnumerable<string> excludeValues = excludeTags.Where(tag => tag.Category == (Category)i).Select(tag => tag.Value);
                if (!includeValues.Any() && !excludeValues.Any()) {
                    continue;
                }
                IEnumerable<string> withoutEmptyStrs = new string[] {
                    string.Join(", ", includeValues),
                    string.Join(", ", excludeValues)
                }.Where(s => !string.IsNullOrEmpty(s));
                displayTexts.Add(((Category)i).ToString() + ": " + string.Join(", ", withoutEmptyStrs));
            }

            return new(
                SEARCH_ADDRESS + string.Join(' ', searchParamStrs.Where(s => !string.IsNullOrEmpty(s))),
                string.Join(Environment.NewLine, displayTexts.Where(s => !string.IsNullOrEmpty(s))),
                (_, arg) => searchLinkItems.Remove((SearchLinkItem)arg.Parameter)
            );
        }
    }
}
