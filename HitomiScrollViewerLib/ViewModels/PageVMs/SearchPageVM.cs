using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HitomiScrollViewerLib.DbContexts;
using HitomiScrollViewerLib.Entities;
using HitomiScrollViewerLib.ViewModels.SearchPageVMs;
using HitomiScrollViewerLib.Views.PageViews;
using Microsoft.EntityFrameworkCore;
using Microsoft.Windows.ApplicationModel.Resources;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        public static SearchPageVM Main => _main ??= new() ;

        public TagFilterEditorVM TagFilterEditorVM { get; } = new();
        public QueryBuilderVM QueryBuilderVM { get; } = new();
        public PairedTFSelectorVM IncludeTFSelectorVM { get; } = new(HitomiContext.Main.TagFilters.Local.ToObservableCollection());
        public PairedTFSelectorVM ExcludeTFSelectorVM { get; } = new(HitomiContext.Main.TagFilters.Local.ToObservableCollection());

        public SyncManagerVM SyncManagerVM { get; } = new();
        public ObservableCollection<SearchFilterVM> SearchFilterVMs { get; } = [];
        public DownloadManagerVM DownloadManagerVM { get; } = DownloadManagerVM.Main;

        [ObservableProperty]
        private string _downloadInputText = "";

        public bool AnyFilterSelected =>
            QueryBuilderVM.GalleryLanguageSelectedIndex > 0 ||
            QueryBuilderVM.GalleryTypeSelectedIndex > 0 ||
            QueryBuilderVM.SearchTitleText.Length > 0 ||
            IncludeTFSelectorVM.AnySelected || ExcludeTFSelectorVM.AnySelected;

        private SearchPageVM() {
            SearchLinkCreateButtonCommand = new RelayCommand(
                HandleSearchLinkCreateButtonClick,
                () => AnyFilterSelected
            );
            DownloadButtonCommand = new RelayCommand(
                HandleDownloadButtonClick,
                () => DownloadInputText.Length != 0
            );
            IncludeTFSelectorVM.OtherTFSSelectorVM = ExcludeTFSelectorVM;
            ExcludeTFSelectorVM.OtherTFSSelectorVM = IncludeTFSelectorVM;

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

        public ICommand SearchLinkCreateButtonCommand { get; }

        public void HandleSearchLinkCreateButtonClick() {
            SearchFilterVM vm = GetSearchFilterVM();
            if (vm != null) {
                // copy link to clipboard
                vm.DeleteCommand.Command = new RelayCommand<SearchFilterVM>((arg) => {
                    SearchFilterVMs.Remove(arg);
                });
                SearchFilterVMs.Add(vm);
                DataPackage dataPackage = new() {
                    RequestedOperation = DataPackageOperation.Copy
                };
                dataPackage.SetText(vm.SearchLink);
                Clipboard.SetContent(dataPackage);
            }
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
            if (TagFilterEditorVM.SelectedTagFilter is not null) {
                HashSet<Tag> currentTags = QueryBuilderVM.GetCurrentTags();
                if (includeTagFilterIds.Contains(TagFilterEditorVM.SelectedTagFilter.Id)) {
                    includeTags = includeTags.Except(TagFilterEditorVM.SelectedTagFilter.Tags).Union(currentTags);
                } else if (excludeTagFilterIds.Contains(TagFilterEditorVM.SelectedTagFilter.Id)) {
                    excludeTags = excludeTags.Except(TagFilterEditorVM.SelectedTagFilter.Tags).Union(currentTags);
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

            if (QueryBuilderVM.GalleryLanguageSelectedIndex > 0) {
                searchFilterVM.GalleryLanguage = QueryBuilderVM.SelectedGalleryLanguage;
            }
            if (QueryBuilderVM.GalleryTypeSelectedIndex > 0) {
                searchFilterVM.GalleryType = QueryBuilderVM.SelectedGalleryTypeEntity;
            }
            if (QueryBuilderVM.SearchTitleText.Length > 0) {
                searchFilterVM.SearchTitleText = QueryBuilderVM.SearchTitleText;
            }

            return searchFilterVM;
        }

        /*
         * Environment.NewLine cannot be used alone as TextBox.Text separator
         * because of this TextBox bug which somehow converts \r\n to \r and it's still not fixed...
         * https://github.com/microsoft/microsoft-ui-xaml/issues/1826
         * https://stackoverflow.com/questions/35138047/uwp-textbox-selectedtext-changes-r-n-to-r
        */
        public static readonly string[] NEW_LINE_SEPS = [Environment.NewLine, "\r"];
        public ICommand DownloadButtonCommand { get; }
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
