using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HitomiScrollViewerLib.DbContexts;
using HitomiScrollViewerLib.Entities;
using HitomiScrollViewerLib.ViewModels.SearchPageVMs;
using HitomiScrollViewerLib.Views.PageViews;
using Microsoft.EntityFrameworkCore;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.ApplicationModel.Resources;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Input;
using Windows.ApplicationModel.DataTransfer;
using static HitomiScrollViewerLib.SharedResources;

namespace HitomiScrollViewerLib.ViewModels.PageVMs {
    public partial class SearchPageVM : DQObservableObject {
        private static readonly ResourceMap _resourceMap = MainResourceMap.GetSubtree(typeof(SearchPage).Name);
        private static readonly Range GALLERY_ID_LENGTH_RANGE = 6..7;

        private static SearchPageVM _main;
        public static SearchPageVM Main => _main ??= new();

        public TagFilterEditorVM TagFilterEditorVM { get; } = new();
        public QueryBuilderVM QueryBuilderVM { get; } = new("SearchPageGalleryLanguageIndex", "SearchPageGalleryTypeIndex");
        public PairedTFSelectorVM IncludeTFSelectorVM { get; } = new(HitomiContext.Main.TagFilters.Local.ToObservableCollection());
        public PairedTFSelectorVM ExcludeTFSelectorVM { get; } = new(HitomiContext.Main.TagFilters.Local.ToObservableCollection());

        public SyncManagerVM SyncManagerVM { get; } = new();
        public ObservableCollection<SearchFilterVM> SearchFilterVMs { get; } = [];
        public DownloadManagerVM DownloadManagerVM { get; } = DownloadManagerVM.Main;

        [ObservableProperty]
        private string _downloadInputText = "";
        partial void OnDownloadInputTextChanged(string value) {
            DownloadButtonCommand.NotifyCanExecuteChanged();
        }

        private SearchPageVM() {
            SearchLinkCreateButtonCommand = new RelayCommand(
                HandleSearchLinkCreateButtonClick,
                () => QueryBuilderVM.AnyQuerySelected || IncludeTFSelectorVM.SelectedTFCBModels.Any() || ExcludeTFSelectorVM.SelectedTFCBModels.Any()
            );
            DownloadButtonCommand = new RelayCommand(
                HandleDownloadButtonClick,
                () => DownloadInputText.Length != 0
            );
            IncludeTFSelectorVM.OtherTFSelectorVM = ExcludeTFSelectorVM;
            ExcludeTFSelectorVM.OtherTFSelectorVM = IncludeTFSelectorVM;
            IncludeTFSelectorVM.CheckBoxToggled += () => SearchLinkCreateButtonCommand.NotifyCanExecuteChanged();
            ExcludeTFSelectorVM.CheckBoxToggled += () => SearchLinkCreateButtonCommand.NotifyCanExecuteChanged();
            QueryBuilderVM.QueryParameterChanged += () => SearchLinkCreateButtonCommand.NotifyCanExecuteChanged();

            TagFilterEditorVM.CurrentTagsRequested += e => e.Tags = QueryBuilderVM.GetCurrentTags();
            TagFilterEditorVM.SelectedTagFilterChanged += selectedTagFilter => {
                QueryBuilderVM.ClearSelectedTags();
                QueryBuilderVM.InsertTags(
                    HitomiContext.Main
                    .TagFilters
                    .Include(tf => tf.Tags)
                    .First(tf => tf.Id == selectedTagFilter.Id)
                    .Tags
                );
            };
        }

        public RelayCommand SearchLinkCreateButtonCommand { get; }

        public void HandleSearchLinkCreateButtonClick() {
            SearchFilterVM vm = GetSearchFilterVM();
            if (vm != null) {
                SearchFilterVMs.Add(vm);
                DataPackage dataPackage = new() {
                    RequestedOperation = DataPackageOperation.Copy
                };
                dataPackage.SetText(vm.SearchLink.AbsoluteUri);
                Clipboard.SetContent(dataPackage);
            }
        }


        public SearchFilterVM GetSearchFilterVM() {
            HashSet<TagFilter> includeTagFilters = IncludeTFSelectorVM.GetSelectedTagFilters().ToHashSet();
            HashSet<TagFilter> excludeTagFilters = ExcludeTFSelectorVM.GetSelectedTagFilters().ToHashSet();
            // if the selected tag filters contain the currently editing (selected) tag filter in ComboBox,
            // replace it with a copied instance where the Tags are replaced with current editing tags
            if (TagFilterEditorVM.SelectedTagFilter is not null) {
                HashSet<Tag> currentTags = QueryBuilderVM.GetCurrentTags();
                if (includeTagFilters.RemoveWhere(t => t.Id == TagFilterEditorVM.SelectedTagFilter.Id) > 0) {
                    includeTagFilters.Add(new() {
                        Id = TagFilterEditorVM.SelectedTagFilter.Id,
                        Name = TagFilterEditorVM.SelectedTagFilter.Name,
                        Tags = currentTags
                    });
                } else if (excludeTagFilters.RemoveWhere(t => t.Id == TagFilterEditorVM.SelectedTagFilter.Id) > 0) {
                    excludeTagFilters.Add(new() {
                        Id = TagFilterEditorVM.SelectedTagFilter.Id,
                        Name = TagFilterEditorVM.SelectedTagFilter.Name,
                        Tags = currentTags
                    });
                }
            }
            IEnumerable<int> includeTagFilterIds = includeTagFilters.Select(tf => tf.Id);
            IEnumerable<int> excludeTagFilterIds = excludeTagFilters.Select(tf => tf.Id);
            HitomiContext.Main.TagFilters
                .Where(tf => includeTagFilterIds.Contains(tf.Id) || excludeTagFilterIds.Contains(tf.Id))
                .Include(tf => tf.Tags)
                .Load();
            HashSet<Tag> includeTags = includeTagFilters.SelectMany(tf => tf.Tags).ToHashSet();
            HashSet<Tag> excludeTags = excludeTagFilters.SelectMany(tf => tf.Tags).ToHashSet();

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
            searchFilterVM.InitInExcludeTagCollections();

            if (QueryBuilderVM.GalleryLanguageSelectedIndex > 0) {
                searchFilterVM.GalleryLanguage = QueryBuilderVM.SelectedGalleryLanguage;
            }
            if (QueryBuilderVM.GalleryTypeSelectedIndex > 0) {
                searchFilterVM.GalleryType = QueryBuilderVM.SelectedGalleryTypeEntity;
            }
            if (QueryBuilderVM.SearchTitleText.Length > 0) {
                searchFilterVM.SearchTitleText = QueryBuilderVM.SearchTitleText;
            }

            searchFilterVM.DeleteCommand.Command = new RelayCommand<SearchFilterVM>((arg) => {
                SearchFilterVMs.Remove(arg);
            });

            return searchFilterVM;
        }

        /*
         * Environment.NewLine cannot be used alone as TextBox.Text separator
         * because of this TextBox bug which somehow converts \r\n to \r and it's still not fixed...
         * https://github.com/microsoft/microsoft-ui-xaml/issues/1826
         * https://stackoverflow.com/questions/35138047/uwp-textbox-selectedtext-changes-r-n-to-r
        */
        public static readonly string[] NEW_LINE_SEPS = [Environment.NewLine, "\r"];
        public RelayCommand DownloadButtonCommand { get; }
        private void HandleDownloadButtonClick() {
            string idPattern = @"\d{" + GALLERY_ID_LENGTH_RANGE.Start + "," + GALLERY_ID_LENGTH_RANGE.End + "}";
            string[] urlOrIds = DownloadInputText.Split(NEW_LINE_SEPS, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (urlOrIds.Length == 0) {
                _ = MainWindowVM.NotifyUser(new() { Title = _resourceMap.GetValue("Notification_DownloadInputTextBox_Empty_Title").ValueAsString });
                return;
            }
            List<int> extractedIds = [];
            foreach (string urlOrId in urlOrIds) {
                MatchCollection matches = Regex.Matches(urlOrId, idPattern);
                if (matches.Count > 0) {
                    extractedIds.Add(int.Parse(matches.Last().Value));
                }
            }
            if (extractedIds.Count == 0) {
                _ = MainWindowVM.NotifyUser(new() { Title = _resourceMap.GetValue("Notification_DownloadInputTextBox_Invalid_Title").ValueAsString });
                return;
            }
            DownloadInputText = "";

            foreach (int id in extractedIds) {
                DownloadManagerVM.TryDownload(id);
            }
        }
    }
}
