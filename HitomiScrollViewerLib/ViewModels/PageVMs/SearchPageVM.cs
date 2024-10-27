using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI;
using HitomiScrollViewerLib.DAOs;
using HitomiScrollViewerLib.DbContexts;
using HitomiScrollViewerLib.Entities;
using HitomiScrollViewerLib.ViewModels.SearchPageVMs;
using HitomiScrollViewerLib.Views.PageViews;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using Windows.ApplicationModel.DataTransfer;

namespace HitomiScrollViewerLib.ViewModels.PageVMs {
    public partial class SearchPageVM : DQObservableObject {
        private static readonly string SUBTREE_NAME = typeof(SearchPage).Name;
        private static readonly Range GALLERY_ID_LENGTH_RANGE = 6..7;

        public static SearchPageVM Main { get; private set; }

        public TagFilterEditorVM TagFilterEditorVM { get; }
        public QueryBuilderVM QueryBuilderVM { get; }
        public PairedTFSelectorVM IncludeTFSelectorVM { get; }
        public PairedTFSelectorVM ExcludeTFSelectorVM { get; }

        public SyncManagerVM SyncManagerVM { get; }
        public ObservableCollection<SearchFilterVM> SearchFilterVMs { get; } = [];
        public DownloadManagerVM DownloadManagerVM { get; } = new();

        [ObservableProperty]
        private string _downloadInputText = "";
        partial void OnDownloadInputTextChanged(string value) {
            DownloadButtonCommand.NotifyCanExecuteChanged();
        }

        private SearchPageVM() {
            TagFilterDAO tagFilterDAO = new();

            TagFilterEditorVM = new(tagFilterDAO);
            QueryBuilderVM = new(PageKind.SearchPage);
            SyncManagerVM = new(tagFilterDAO);
            IncludeTFSelectorVM = new(tagFilterDAO);
            ExcludeTFSelectorVM = new(tagFilterDAO);

            SearchLinkCreateButtonCommand = new RelayCommand(
                HandleSearchLinkCreateButtonClick,
                () => QueryBuilderVM.AnyQuerySelected || IncludeTFSelectorVM.AnySelected() || ExcludeTFSelectorVM.AnySelected()
            );
            DownloadButtonCommand = new RelayCommand(
                HandleDownloadButtonClick,
                () => DownloadInputText.Length != 0
            );
            IncludeTFSelectorVM.OtherTFSelectorVM = ExcludeTFSelectorVM;
            ExcludeTFSelectorVM.OtherTFSelectorVM = IncludeTFSelectorVM;
            IncludeTFSelectorVM.CheckBoxToggled += () => SearchLinkCreateButtonCommand.NotifyCanExecuteChanged();
            ExcludeTFSelectorVM.CheckBoxToggled += () => SearchLinkCreateButtonCommand.NotifyCanExecuteChanged();
            QueryBuilderVM.QueryChanged += () => SearchLinkCreateButtonCommand.NotifyCanExecuteChanged();

            TagFilterEditorVM.CurrentTagsRequested += e => e.Tags = QueryBuilderVM.GetCurrentTags();
            TagFilterEditorVM.SelectedTagFilterChanged += selectedTagFilter => {
                QueryBuilderVM.ClearSelectedTags();
                QueryBuilderVM.InsertTags(selectedTagFilter.Tags);
            };

            // select the default
            if (tagFilterDAO.LocalTagFilters.Count > 0) {
                TagFilterEditorVM.SelectedTagFilter = tagFilterDAO.LocalTagFilters.Last();
            }
        }

        public static void Init() {
            Main = new();
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
            HashSet<TagFilter> includeTagFilters = [.. IncludeTFSelectorVM.GetSelectedTagFilters()];
            HashSet<TagFilter> excludeTagFilters = [.. ExcludeTFSelectorVM.GetSelectedTagFilters()];
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
            HashSet<int> includeTagFilterIds = [.. includeTagFilters.Select(tf => tf.Id)];
            HashSet<int> excludeTagFilterIds = [.. excludeTagFilters.Select(tf => tf.Id)];

            using HitomiContext context = new();
            HashSet<Tag> includeTags = [.. context.TagFilters.AsNoTracking().Where(tf => includeTagFilterIds.Contains(tf.Id)).SelectMany(tf => tf.Tags)];
            HashSet<Tag> excludeTags = [.. context.TagFilters.AsNoTracking().Where(tf => excludeTagFilterIds.Contains(tf.Id)).SelectMany(tf => tf.Tags)];

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
                    Title = "Notification_DuplicateTags".GetLocalized(SUBTREE_NAME),
                    Message = string.Join(Environment.NewLine, dupTagStrs)
                });
                return null;
            }

            SearchFilterVM searchFilterVM = new() {
                GalleryLanguage = QueryBuilderVM.QueryConfiguration.SelectedLanguage,
                GalleryType = QueryBuilderVM.QueryConfiguration.SelectedType,
                SearchTitleText = QueryBuilderVM.SearchTitleText,
                IncludeTags = includeTags,
                ExcludeTags = excludeTags,
            };
            searchFilterVM.InitInExcludeTagCollections();

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
            List<int> extractedIds = [];
            foreach (string urlOrId in urlOrIds) {
                MatchCollection matches = Regex.Matches(urlOrId, idPattern);
                if (matches.Count > 0) {
                    extractedIds.Add(int.Parse(matches.Last().Value));
                }
            }
            if (extractedIds.Count == 0) {
                _ = MainWindowVM.NotifyUser(new() { Title = "Notification_InvalidInput".GetLocalized(SUBTREE_NAME) });
                return;
            }
            DownloadInputText = "";

            foreach (int id in extractedIds) {
                DownloadManagerVM.TryDownload(id);
            }
        }
    }
}
