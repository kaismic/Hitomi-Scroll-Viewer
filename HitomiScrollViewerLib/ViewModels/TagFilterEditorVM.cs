using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HitomiScrollViewerLib.DbContexts;
using HitomiScrollViewerLib.Entities;
using HitomiScrollViewerLib.ViewModels.SearchPageVMs;
using HitomiScrollViewerLib.Views;
using Microsoft.EntityFrameworkCore;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.ApplicationModel.Resources;
using Soluling;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Windows.Storage;
using static HitomiScrollViewerLib.SharedResources;

namespace HitomiScrollViewerLib.ViewModels {
    public partial class TagFilterEditorVM : ObservableObject {
        private static readonly ResourceMap _resourceMap = MainResourceMap.GetSubtree(typeof(TagFilterEditor).Name);
        private const string SEARCH_ADDRESS = "https://hitomi.la/search.html?";
        private static readonly Dictionary<TagCategory, string> CATEGORY_SEARCH_PARAM_DICT = new() {
            { TagCategory.Tag, "tag" },
            { TagCategory.Male, "male" },
            { TagCategory.Female, "female" },
            { TagCategory.Artist, "artist" },
            { TagCategory.Group, "group" },
            { TagCategory.Character, "character" },
            { TagCategory.Series, "series" }
        };

        private static event PropertyChangedEventHandler StaticPropertyChanged;
        private static void NotifyStaticPropertyChanged([CallerMemberName] string name = null) {
            StaticPropertyChanged?.Invoke(null, new PropertyChangedEventArgs(name));
        }

        [ObservableProperty]
        private int _galleryTypeSelectedIndex;
        public GalleryTypeEntity[] GalleryTypeEntities { get; } =
            Enumerable.Concat(
                [new GalleryTypeEntity() { GalleryType = null }],
                HitomiContext.Main.GalleryTypes
            ).ToArray();

        [ObservableProperty]
        private int _galleryLanguageSelectedIndex;
        public GalleryLanguage[] GalleryLanguages { get; } =
            Enumerable.Concat(
                [new GalleryLanguage() { LocalName = TEXT_ALL }],
                HitomiContext.Main.GalleryLanguages.OrderBy(gl => gl.LocalName)
            ).ToArray();


        private static bool s_isAutoSaveEnabled = (bool)(ApplicationData.Current.LocalSettings.Values[AUTO_SAVE_SETTING_KEY] ?? true);
        public bool IsAutoSaveEnabled {
            get => s_isAutoSaveEnabled;
            set {
                if (s_isAutoSaveEnabled != value) {
                    s_isAutoSaveEnabled = value;
                    ApplicationData.Current.LocalSettings.Values[AUTO_SAVE_SETTING_KEY] = value;
                    NotifyStaticPropertyChanged();
                    OnPropertyChanged();
                }
            }
        }

        private static readonly string AUTO_SAVE_SETTING_KEY = "AutoSave";
        public string AutoSaveCheckBoxText { get; } = _resourceMap.GetValue("AutoSaveCheckBox_Text").ValueAsString;

        public TagTokenizingTextBoxVM[] TttVMs { get; } = new TagTokenizingTextBoxVM[Tag.TAG_CATEGORIES.Length];

        [ObservableProperty]
        private string _searchTitleText;

        [ObservableProperty]
        private TagFilter _selectedTagFilter;


        public bool AnyFilterSelected {
            get => GalleryLanguageSelectedIndex > 0 ||
                GalleryTypeSelectedIndex > 0 ||
                SearchTitleText.Length > 0 ||
                IncludeTFSelectorVM.AnySelected || ExcludeTFSelectorVM.AnySelected;
        }

        public PairedTFSelectorVM IncludeTFSelectorVM { get; } = new(HitomiContext.Main.TagFilters.Local.ToObservableCollection());
        public PairedTFSelectorVM ExcludeTFSelectorVM { get; } = new(HitomiContext.Main.TagFilters.Local.ToObservableCollection());

        private HashSet<int> DeletedTagFilterIds { get; set; }

        private TagFilterEditorVM() {
            IncludeTFSelectorVM.OtherTFSSelectorVM = ExcludeTFSelectorVM;
            ExcludeTFSelectorVM.OtherTFSSelectorVM = IncludeTFSelectorVM;

            StaticPropertyChanged += (object _0, PropertyChangedEventArgs e) => {
                OnPropertyChanged(e.PropertyName);
            };

            for (int i = 0; i < Tag.TAG_CATEGORIES.Length; i++) {
                TttVMs[i] = new((TagCategory)i);
            }
        }

        private HashSet<Tag> GetCurrentTags() {
            return
                Enumerable
                .Range(0, Tag.TAG_CATEGORIES.Length)
                .Select(i => TttVMs[i].SelectedTags)
                .SelectMany(tags => tags)
                .ToHashSet();
        }

        private void TagFilterComboBox_SelectionChanged(object _0, SelectionChangedEventArgs e) {
            if (e.RemovedItems.Count > 0 && e.RemovedItems[0] is TagFilter prevTagFilter && IsAutoSaveEnabled) {
                // do not save if this selection change occurred due to deletion of currently selected tag filter
                if (DeletedTagFilterIds == null) {
                    SaveTagFilter(prevTagFilter);
                } else if (!DeletedTagFilterIds.Contains(prevTagFilter.Id)) {
                    SaveTagFilter(prevTagFilter);
                }
            }
            foreach (var vm in TttVMs) {
                vm.SelectedTags.Clear();
            }

            ICollection<Tag> selectedTagFilterTags = HitomiContext.Main
                .TagFilters
                .Include(tf => tf.Tags)
                .First(tf => tf.Id == SelectedTagFilter.Id)
                .Tags;
            foreach (Tag tag in selectedTagFilterTags) {
                TttVMs[(int)tag.Category].SelectedTags.Add(tag);
            }
        }

        public ICommand CreateButtonCommand => new RelayCommand(CreateButton_Click);

        private async void CreateButton_Click() {
            CRUDContentDialogVM cdvm = new(CRUDContentDialogVM.CRUDAction.Create);
            CRUDContentDialog cd = new() { ViewModel = cdvm };
            if (await cd.ShowAsync() != ContentDialogResult.Primary) {
                return;
            }
            string name = cdvm.GetInputText();
            TagFilter tf = new() {
                Name = name,
                Tags = GetCurrentTags()
            };
            HitomiContext.Main.TagFilters.Add(tf);
            HitomiContext.Main.SaveChanges();
            SelectedTagFilter = tf;
            MainWindowVM.Main.ShowPopup(
                string.Format(
                    _resourceMap.GetValue("InfoBar_Message_Create_Complete").ValueAsString,
                    name
                )
            );
        }

        public ICommand RenameButtonCommand => new RelayCommand(
            RenameButton_Click,
            () => SelectedTagFilter != null
        );

        private async void RenameButton_Click() {
            string oldName = SelectedTagFilter.Name;
            CRUDContentDialogVM cdvm = new(CRUDContentDialogVM.CRUDAction.Rename, oldName);
            CRUDContentDialog cd = new() { ViewModel = cdvm };
            if (await cd.ShowAsync() != ContentDialogResult.Primary) {
                return;
            }
            string newName = cdvm.GetInputText();
            SelectedTagFilter.Name = newName;
            HitomiContext.Main.SaveChanges();
            MainWindowVM.Main.ShowPopup(
                string.Format(
                    _resourceMap.GetValue("InfoBar_Message_Rename_Complete").ValueAsString,
                    oldName,
                    newName
                )
            );
        }


        public ICommand SaveButtonCommand => new RelayCommand(
            SaveButton_Click,
            () => SelectedTagFilter != null
        );

        private void SaveTagFilter(TagFilter tf) {
            tf.Tags = GetCurrentTags();
            HitomiContext.Main.SaveChanges();
            MainWindowVM.Main.ShowPopup(
                string.Format(
                    _resourceMap.GetValue("InfoBar_Message_Save_Complete").ValueAsString,
                    tf.Name
                )
            );
        }

        private void SaveButton_Click() {
            SaveTagFilter(SelectedTagFilter);
        }

        public ICommand DeleteButtonCommand => new RelayCommand(DeleteButton_Click);

        private async void DeleteButton_Click() {
            CRUDContentDialogVM cdvm = new(CRUDContentDialogVM.CRUDAction.Delete);
            CRUDContentDialog cd = new() { ViewModel = cdvm };
            if (await cd.ShowAsync() != ContentDialogResult.Primary) {
                return;
            }
            IEnumerable<TagFilter> SelectedTagFilters = cdvm.GetSelectedTagFilters();
            DeletedTagFilterIds = SelectedTagFilters.Select(tf => tf.Id).ToHashSet();
            HitomiContext.Main.TagFilters.RemoveRange(cdvm.GetSelectedTagFilters());
            HitomiContext.Main.SaveChanges();
            MainWindowVM.Main.ShowPopup(
                MultiPattern.Format(
                    _resourceMap.GetValue("InfoBar_Message_Delete_Complete").ValueAsString,
                    DeletedTagFilterIds.Count
                )
            );
        }

        private (IEnumerable<Tag> includeTags, IEnumerable<Tag> excludeTags)? GetValidatedTags() {
            // check if all selected tag filters are all empty
            if (!AnyFilterSelected) {
                MainWindowVM.NotifyUser(new() {
                    Title = _resourceMap.GetValue("Notification_Selected_TagFilterSets_Empty_Title").ValueAsString
                });
                return null;
            }

            IEnumerable<TagFilter> includeTagFilters = IncludeTFSelectorVM.GetSelectedTagFilters();
            IEnumerable<TagFilter> excludeTagFilters = ExcludeTFSelectorVM.GetSelectedTagFilters();
            IEnumerable<int> includeTagFilterIds = includeTagFilters.Select(tf => tf.Id);
            IEnumerable<int> excludeTagFilterIds = excludeTagFilters.Select(tf => tf.Id);
            HitomiContext.Main.TagFilters
                .Where(tf => includeTagFilterIds.Contains(tf.Id) || excludeTagFilterIds.Contains(tf.Id))
                .Include(tf => tf.Tags)
                .Load();
            IEnumerable<Tag> includeTags = includeTagFilters.SelectMany(tf => tf.Tags);
            IEnumerable<Tag> excludeTags = excludeTagFilters.SelectMany(tf => tf.Tags);

            // update to use current tags in text boxes if the selected tag filters contain the combobox selected tag filter
            if (SelectedTagFilter != null) {
                HashSet<Tag> currentTags = GetCurrentTags();
                if (includeTagFilterIds.Contains(SelectedTagFilter.Id)) {
                    includeTags = includeTags.Except(SelectedTagFilter.Tags).Union(currentTags);
                } else if (excludeTagFilterIds.Contains(SelectedTagFilter.Id)) {
                    excludeTags = excludeTags.Except(SelectedTagFilter.Tags).Union(currentTags);
                }
            }

            IEnumerable<Tag> dupTags = includeTags.Intersect(excludeTags);
            if (dupTags.Any()) {
                List<string> dupTagStrs = new(Tag.TAG_CATEGORIES.Length);
                foreach (TagCategory category in Tag.TAG_CATEGORIES) {
                    IEnumerable<string> dupStrs = dupTags.Where(tag => tag.Category == category).Select(tag => tag.Value);
                    if (!dupStrs.Any()) {
                        continue;
                    }
                    dupTagStrs.Add(category.ToString() + ": " + string.Join(", ", dupStrs));
                }
                MainWindowVM.NotifyUser(new() {
                    Title = _resourceMap.GetValue("Notification_Duplicate_Tags_Title").ValueAsString,
                    Message = string.Join(Environment.NewLine, dupTagStrs)
                });
                return null;
            }

            return (includeTags, excludeTags);
        }

        public SearchLinkItemVM GetSearchLinkItemVM() {
            (IEnumerable<Tag> includeTags, IEnumerable<Tag> excludeTags)? tags = GetValidatedTags();
            if (!tags.HasValue) {
                return null;
            }

            List<string> searchParamStrs = new(5); // 5 = include + exclude + language, type and title search
            List<string> displayTexts = new(Tag.TAG_CATEGORIES.Length + 3); // + 3 for language, type and title search

            if (GalleryLanguageSelectedIndex > 0) {
                searchParamStrs.Add("language:" + GalleryLanguages[GalleryLanguageSelectedIndex].SearchParamValue);
                displayTexts.Add(TEXT_LANGUAGE + ": " + GalleryLanguages[GalleryLanguageSelectedIndex].LocalName);
            }

            if (GalleryTypeSelectedIndex > 0) {
                searchParamStrs.Add("type:" + GalleryTypeEntities[GalleryTypeSelectedIndex].SearchParamValue);
                displayTexts.Add(TEXT_TYPE + ": " + GalleryTypeEntities[GalleryTypeSelectedIndex].DisplayName);
            }

            // add joined include exclude search param strs
            searchParamStrs.Add(string.Join(' ', tags.Value.includeTags.Select(tag => CATEGORY_SEARCH_PARAM_DICT[tag.Category] + ':' + tag.SearchParamValue)));
            searchParamStrs.Add(string.Join(' ', tags.Value.excludeTags.Select(tag => '-' + CATEGORY_SEARCH_PARAM_DICT[tag.Category] + ':' + tag.SearchParamValue)));

            // add display texts for each tag category
            foreach (TagCategory category in Tag.TAG_CATEGORIES) {
                IEnumerable<string> includeValues = tags.Value.includeTags.Where(tag => tag.Category == category).Select(tag => tag.Value);
                IEnumerable<string> excludeValues = tags.Value.excludeTags.Where(tag => tag.Category == category).Select(tag => tag.Value);
                if (!includeValues.Any() && !excludeValues.Any()) {
                    continue;
                }
                IEnumerable<string> withoutEmptyStrs = new string[] {
                    string.Join(", ", includeValues),
                    string.Join(", ", excludeValues)
                }.Where(s => !string.IsNullOrEmpty(s));
                displayTexts.Add((category).ToString() + ": " + string.Join(", ", withoutEmptyStrs));
            }

            if (SearchTitleText.Length > 0) {
                searchParamStrs.Add(SearchTitleText);
                displayTexts.Add(SearchTitleText);
            }

            return new(
                SEARCH_ADDRESS + string.Join(' ', searchParamStrs.Where(s => !string.IsNullOrEmpty(s))),
                string.Join(Environment.NewLine, displayTexts.Where(s => !string.IsNullOrEmpty(s)))
            );
        }

        public IEnumerable<Gallery> GetFilteredGalleries() {
            (IEnumerable<Tag> includeTags, IEnumerable<Tag> excludeTags)? tags = GetValidatedTags();
            if (!tags.HasValue) {
                return null;
            }
            IEnumerable<Gallery> filtered = HitomiContext.Main.Galleries;
            if (GalleryLanguageSelectedIndex > 0) {
                filtered = filtered.Where(g => g.GalleryLanguage.Id == GalleryLanguages[GalleryLanguageSelectedIndex].Id);
            }
            if (GalleryTypeSelectedIndex > 0) {
                filtered = filtered.Where(g => g.GalleryType.Id == GalleryTypeEntities[GalleryTypeSelectedIndex].Id);
            }
            if (SearchTitleText.Length > 0) {
                filtered = filtered.Where(g => g.Title.Contains(SearchTitleText));
            }
            foreach (Tag includeTag in tags.Value.includeTags) {
                filtered = filtered.Intersect(includeTag.Galleries);
            }
            foreach (Tag excludeTag in tags.Value.includeTags) {
                filtered = filtered.Except(excludeTag.Galleries);
            }
            return filtered;
        }
    }
}
