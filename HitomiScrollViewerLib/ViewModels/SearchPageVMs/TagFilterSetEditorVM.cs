using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HitomiScrollViewerLib.DbContexts;
using HitomiScrollViewerLib.Entities;
using HitomiScrollViewerLib.Views;
using HitomiScrollViewerLib.Views.SearchPageViews;
using Microsoft.EntityFrameworkCore;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.ApplicationModel.Resources;
using Soluling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Windows.Storage;
using static HitomiScrollViewerLib.SharedResources;

namespace HitomiScrollViewerLib.ViewModels.SearchPageVMs {
    public partial class TagFilterSetEditorVM : ObservableObject, IAppWindowClosingHandler {
        private static readonly ResourceMap _resourceMap = MainResourceMap.GetSubtree(typeof(TagFilterSetEditor).Name);
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

        [ObservableProperty]
        private int _galleryTypeSelectedIndex;
        public GalleryTypeEntity[] GalleryTypeEntities =>
            Enumerable.Concat(
                [new GalleryTypeEntity() { GalleryType = null }],
                HitomiContext.Main.GalleryTypes
            ).ToArray();

        [ObservableProperty]
        private int _galleryLanguageSelectedIndex;
        public GalleryLanguage[] GalleryLanguages =>
            Enumerable.Concat(
                [new GalleryLanguage() { LocalName = TEXT_ALL }],
                HitomiContext.Main.GalleryLanguages.OrderBy(gl => gl.LocalName)
            ).ToArray();

        [ObservableProperty]
        private bool _isAutoSaveEnabled;
        private static readonly string AUTO_SAVE_SETTING_KEY = "AutoSave";
        public string AutoSaveCheckBoxText => _resourceMap.GetValue("AutoSaveCheckBox_Text").ValueAsString;

        public TagTokenizingTextBoxVM[] TTTextBoxVMs => new TagTokenizingTextBoxVM[Tag.TAG_CATEGORIES.Length];

        [ObservableProperty]
        private string _extraKeywordsText;
        partial void OnIsAutoSaveEnabledChanged(bool value) {
            ApplicationData.Current.LocalSettings.Values[AUTO_SAVE_SETTING_KEY] = value;
        }
        [ObservableProperty]
        private TagFilterSet _selectedTFS;


        private bool HyperlinkCreateButtonEnabled {
            get => GalleryLanguageSelectedIndex > 0 ||
                GalleryTypeSelectedIndex > 0 ||
                ExtraKeywordsText.Length > 0 ||
                IncludeTFSSelectorVM.AnySelected || ExcludeTFSSelectorVM.AnySelected;
        }

        public PairedTFSSelectorVM IncludeTFSSelectorVM { get; } = new(HitomiContext.Main.TagFilterSets.Local.ToObservableCollection());
        public PairedTFSSelectorVM ExcludeTFSSelectorVM { get; } = new(HitomiContext.Main.TagFilterSets.Local.ToObservableCollection());

        private HashSet<int> DeletedTFSIds { get; set; }

        public TagFilterSetEditorVM() {
            IncludeTFSSelectorVM.OtherTFSSelectorVM = ExcludeTFSSelectorVM;
            ExcludeTFSSelectorVM.OtherTFSSelectorVM = IncludeTFSSelectorVM;

            IsAutoSaveEnabled = (bool)(ApplicationData.Current.LocalSettings.Values[AUTO_SAVE_SETTING_KEY] ?? true);

            for (int i = 0; i < Tag.TAG_CATEGORIES.Length; i++) {
                TTTextBoxVMs[i] = new((TagCategory)i);
            }
        }

        public void HandleAppWindowClosing(AppWindowClosingEventArgs _0) {
            if (SelectedTFS != null) {
                SaveTFS(SelectedTFS);
            }
        }

        private HashSet<Tag> GetCurrentTags() {
            return
                Enumerable
                .Range(0, Tag.TAG_CATEGORIES.Length)
                .Select(i => TTTextBoxVMs[i].SelectedTags)
                .SelectMany(tags => tags)
                .ToHashSet();
        }

        private void TagFilterSetComboBox_SelectionChanged(object _0, SelectionChangedEventArgs e) {
            if (e.RemovedItems.Count > 0 && e.RemovedItems[0] is TagFilterSet prevTFS && IsAutoSaveEnabled) {
                // do not save if this selection change occurred due to deletion of currently selected tfs
                if (DeletedTFSIds == null) {
                    SaveTFS(prevTFS);
                } else if (!DeletedTFSIds.Contains(prevTFS.Id)) {
                    SaveTFS(prevTFS);
                }
            }
            foreach (var vm in TTTextBoxVMs) {
                vm.SelectedTags.Clear();
            }

            ICollection<Tag> selectedTFSTags = HitomiContext.Main
                .TagFilterSets
                .Include(tfs => tfs.Tags)
                .First(tfs => tfs.Id == SelectedTFS.Id)
                .Tags;
            foreach (Tag tag in selectedTFSTags) {
                TTTextBoxVMs[(int)tag.Category].SelectedTags.Add(tag);
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
            TagFilterSet tfs = new() {
                Name = name,
                Tags = GetCurrentTags()
            };
            HitomiContext.Main.TagFilterSets.Add(tfs);
            HitomiContext.Main.SaveChanges();
            SelectedTFS = tfs;
            MainWindow.SearchPageVM.AddPopupMsgInfoBarVM(
                string.Format(
                    _resourceMap.GetValue("InfoBar_Message_Create_Complete").ValueAsString,
                    name
                )
            );
        }

        public ICommand RenameButtonCommand => new RelayCommand(
            RenameButton_Click,
            () => SelectedTFS != null
        );

        private async void RenameButton_Click() {
            string oldName = SelectedTFS.Name;
            CRUDContentDialogVM cdvm = new(CRUDContentDialogVM.CRUDAction.Rename, oldName);
            CRUDContentDialog cd = new() { ViewModel = cdvm };
            if (await cd.ShowAsync() != ContentDialogResult.Primary) {
                return;
            }
            string newName = cdvm.GetInputText();
            SelectedTFS.Name = newName;
            HitomiContext.Main.SaveChanges();
            MainWindow.SearchPageVM.AddPopupMsgInfoBarVM(
                string.Format(
                    _resourceMap.GetValue("InfoBar_Message_Rename_Complete").ValueAsString,
                    oldName,
                    newName
                )
            );
        }


        public ICommand SaveButtonCommand => new RelayCommand(
            SaveButton_Click,
            () => SelectedTFS != null
        );

        private void SaveTFS(TagFilterSet tfs) {
            tfs.Tags = GetCurrentTags();
            HitomiContext.Main.SaveChanges();
            MainWindow.SearchPageVM.AddPopupMsgInfoBarVM(
                string.Format(
                    _resourceMap.GetValue("InfoBar_Message_Save_Complete").ValueAsString,
                    tfs.Name
                )
            );
        }

        private void SaveButton_Click() {
            SaveTFS(SelectedTFS);
        }

        public ICommand DeleteButtonCommand => new RelayCommand(DeleteButton_Click);

        private async void DeleteButton_Click() {
            CRUDContentDialogVM cdvm = new(CRUDContentDialogVM.CRUDAction.Delete);
            CRUDContentDialog cd = new() { ViewModel = cdvm };
            if (await cd.ShowAsync() != ContentDialogResult.Primary) {
                return;
            }
            IEnumerable<TagFilterSet> selectedTFSs = cdvm.GetSelectedTFSs();
            DeletedTFSIds = selectedTFSs.Select(tfs => tfs.Id).ToHashSet();
            HitomiContext.Main.TagFilterSets.RemoveRange(cdvm.GetSelectedTFSs());
            HitomiContext.Main.SaveChanges();
            MainWindow.SearchPageVM.AddPopupMsgInfoBarVM(
                MultiPattern.Format(
                    _resourceMap.GetValue("InfoBar_Message_Delete_Complete").ValueAsString,
                    DeletedTFSIds.Count
                )
            );
        }

        internal SearchLinkItemVM GetSearchLinkItemVM() {
            IEnumerable<TagFilterSet> includeTFSs = IncludeTFSSelectorVM.GetSelectedTFSs();
            IEnumerable<TagFilterSet> excludeTFSs = ExcludeTFSSelectorVM.GetSelectedTFSs();
            IEnumerable<int> includeTFSIds = includeTFSs.Select(tfs => tfs.Id);
            IEnumerable<int> excludeTFSIds = excludeTFSs.Select(tfs => tfs.Id);
            HitomiContext.Main.TagFilterSets
                .Where(tfs => includeTFSIds.Contains(tfs.Id) || excludeTFSIds.Contains(tfs.Id))
                .Include(tfs => tfs.Tags)
                .Load();
            IEnumerable<Tag> includeTags = includeTFSs.SelectMany(tfs => tfs.Tags);
            IEnumerable<Tag> excludeTags = excludeTFSs.SelectMany(tfs => tfs.Tags);

            // check if all selected TFSs are all empty
            if (!includeTags.Any() && !excludeTags.Any() &&
                GalleryLanguageSelectedIndex <= 0 &&
                GalleryTypeSelectedIndex <= 0 &&
                ExtraKeywordsText.Length == 0
            ) {
                MainWindow.CurrMW.NotifyUser(
                    _resourceMap.GetValue("Notification_Selected_TagFilterSets_Empty_Title").ValueAsString,
                    ""
                );
                return null;
            }

            // update to use current tags in text boxes if the selected TFSs contain the combobox selected tfs
            if (SelectedTFS != null) {
                HashSet<Tag> currentTags = GetCurrentTags();
                if (includeTFSIds.Contains(SelectedTFS.Id)) {
                    includeTags = includeTags.Except(SelectedTFS.Tags).Union(currentTags);
                } else if (excludeTFSIds.Contains(SelectedTFS.Id)) {
                    excludeTags = excludeTags.Except(SelectedTFS.Tags).Union(currentTags);
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
                MainWindow.CurrMW.NotifyUser(
                    _resourceMap.GetValue("Notification_Duplicate_Tags_Title").ValueAsString,
                    string.Join(Environment.NewLine, dupTagStrs)
                );
                return null;
            }

            List<string> searchParamStrs = new(5); // 5 = include + exclude + language, type and extra keywords
            List<string> displayTexts = new(Tag.TAG_CATEGORIES.Length + 3); // + 3 for language, type and extra keywords

            if (GalleryLanguageSelectedIndex > 0) {
                searchParamStrs.Add("language:" + GalleryLanguages[GalleryLanguageSelectedIndex].SearchParamValue);
                displayTexts.Add("Language: " + GalleryLanguages[GalleryLanguageSelectedIndex].LocalName);
            }

            if (GalleryTypeSelectedIndex > 0) {
                searchParamStrs.Add("type:" + GalleryTypeEntities[GalleryTypeSelectedIndex].SearchParamValue);
                displayTexts.Add("Type: " + GalleryTypeEntities[GalleryTypeSelectedIndex].DisplayName);
            }

            // add joined include exclude search param strs
            searchParamStrs.Add(string.Join(' ', includeTags.Select(tag => CATEGORY_SEARCH_PARAM_DICT[tag.Category] + ':' + tag.SearchParamValue)));
            searchParamStrs.Add(string.Join(' ', excludeTags.Select(tag => '-' + CATEGORY_SEARCH_PARAM_DICT[tag.Category] + ':' + tag.SearchParamValue)));

            // add display texts for each tag category
            foreach (TagCategory category in Tag.TAG_CATEGORIES) {
                IEnumerable<string> includeValues = includeTags.Where(tag => tag.Category == category).Select(tag => tag.Value);
                IEnumerable<string> excludeValues = excludeTags.Where(tag => tag.Category == category).Select(tag => tag.Value);
                if (!includeValues.Any() && !excludeValues.Any()) {
                    continue;
                }
                IEnumerable<string> withoutEmptyStrs = new string[] {
                    string.Join(", ", includeValues),
                    string.Join(", ", excludeValues)
                }.Where(s => !string.IsNullOrEmpty(s));
                displayTexts.Add((category).ToString() + ": " + string.Join(", ", withoutEmptyStrs));
            }

            if (ExtraKeywordsText.Length > 0) {
                searchParamStrs.Add(ExtraKeywordsText);
                displayTexts.Add("Keywords: " + ExtraKeywordsText);
            }

            return new(
                SEARCH_ADDRESS + string.Join(' ', searchParamStrs.Where(s => !string.IsNullOrEmpty(s))),
                string.Join(Environment.NewLine, displayTexts.Where(s => !string.IsNullOrEmpty(s)))
            );
        }
    }
}
