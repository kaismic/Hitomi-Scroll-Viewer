using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HitomiScrollViewerLib.DbContexts;
using HitomiScrollViewerLib.Entities;
using HitomiScrollViewerLib.Models;
using HitomiScrollViewerLib.Views;
using Microsoft.EntityFrameworkCore;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.ApplicationModel.Resources;
using Soluling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using static HitomiScrollViewerLib.SharedResources;

namespace HitomiScrollViewerLib.ViewModels {
    public partial class TagFilterEditorVM : ObservableObject {
        private static readonly ResourceMap _resourceMap = MainResourceMap.GetSubtree(typeof(TagFilterEditor).Name);

        [ObservableProperty]
        private int _galleryTypeSelectedIndex;
        public GalleryTypeEntity[] GalleryTypeEntities { get; } =
            [new GalleryTypeEntity() { GalleryType = null }, .. HitomiContext.Main.GalleryTypes];

        [ObservableProperty]
        private int _galleryLanguageSelectedIndex;
        public GalleryLanguage[] GalleryLanguages { get; } =
            [new GalleryLanguage() { LocalName = TEXT_ALL }, .. HitomiContext.Main.GalleryLanguages.OrderBy(gl => gl.LocalName)];


        public string AutoSaveCheckBoxText { get; } = _resourceMap.GetValue("AutoSaveCheckBox_Text").ValueAsString;
        public CommonSettings CommonSettings { get; } = CommonSettings.Main;

        public TagTokenizingTextBoxVM[] TttVMs { get; } = new TagTokenizingTextBoxVM[Tag.TAG_CATEGORIES.Length];

        [ObservableProperty]
        private string _searchTitleText;

        [ObservableProperty]
        private TagFilter _selectedTagFilter;
        partial void OnSelectedTagFilterChanged(TagFilter oldValue, TagFilter newValue) {
            if (oldValue is TagFilter && CommonSettings.IsTFAutoSaveEnabled) {
                // do not save if this selection change occurred due to deletion of currently selected tag filter
                if (DeletedTagFilterIds == null) {
                    SaveTagFilter(oldValue);
                } else if (!DeletedTagFilterIds.Contains(oldValue.Id)) {
                    SaveTagFilter(oldValue);
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


        public bool AnyFilterSelected {
            get => GalleryLanguageSelectedIndex > 0 ||
                GalleryTypeSelectedIndex > 0 ||
                SearchTitleText.Length > 0 ||
                IncludeTFSelectorVM.AnySelected || ExcludeTFSelectorVM.AnySelected;
        }

        public PairedTFSelectorVM IncludeTFSelectorVM { get; } = new(HitomiContext.Main.TagFilters.Local.ToObservableCollection());
        public PairedTFSelectorVM ExcludeTFSelectorVM { get; } = new(HitomiContext.Main.TagFilters.Local.ToObservableCollection());

        private HashSet<int> DeletedTagFilterIds { get; set; }

        public GalleryViewSettings ViewSettingsModel { get; set; }

        private TagFilterEditorVM() {
            IncludeTFSelectorVM.OtherTFSSelectorVM = ExcludeTFSelectorVM;
            ExcludeTFSelectorVM.OtherTFSSelectorVM = IncludeTFSelectorVM;

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
            MainWindowVM.ShowPopup(
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
            MainWindowVM.ShowPopup(
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
            MainWindowVM.ShowPopup(
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
            MainWindowVM.ShowPopup(
                MultiPattern.Format(
                    _resourceMap.GetValue("InfoBar_Message_Delete_Complete").ValueAsString,
                    DeletedTagFilterIds.Count
                )
            );
        }

        public SearchFilterVM GetSearchFilterVM() {
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
                _ = MainWindowVM.NotifyUser(new() {
                    Title = _resourceMap.GetValue("Notification_Duplicate_Tags_Title").ValueAsString,
                    Message = string.Join(Environment.NewLine, dupTagStrs)
                });
                return null;
            }

            SearchFilterVM searchFilterVM = new() {
                 IncludeTags = includeTags,
                 ExcludeTags = excludeTags,
            };
            searchFilterVM.InitSearchFilterTagsRepeaterVMs();

            if (GalleryLanguageSelectedIndex > 0) {
                searchFilterVM.GalleryLanguage = GalleryLanguages[GalleryLanguageSelectedIndex];
            }
            if (GalleryTypeSelectedIndex > 0) {
                searchFilterVM.GalleryType = GalleryTypeEntities[GalleryTypeSelectedIndex];
            }
            if (SearchTitleText.Length > 0) {
                searchFilterVM.SearchTitleText = SearchTitleText;
            }

            return searchFilterVM;
        }
    }
}
