using HitomiScrollViewerData;
using HitomiScrollViewerData.Builders;
using HitomiScrollViewerData.DTOs;
using HitomiScrollViewerWebApp.Components;
using HitomiScrollViewerWebApp.Components.Dialogs;
using HitomiScrollViewerWebApp.Models;
using HitomiScrollViewerWebApp.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Text.Json;
using static HitomiScrollViewerData.Entities.Tag;

namespace HitomiScrollViewerWebApp.Pages {
    public partial class SearchPage {
        private static readonly Action<SnackbarOptions> SNACKBAR_OPTIONS = options => {
            options.ShowCloseIcon = true;
            options.CloseAfterNavigation = true;
            options.ShowTransitionDuration = 0;
            options.HideTransitionDuration = 500;
            options.VisibleStateDuration = 3000;
            options.DuplicatesBehavior = SnackbarDuplicatesBehavior.Allow;
        };

        private ObservableCollection<TagFilterDTO> _tagFilters = [];
        public ObservableCollection<TagFilterDTO> TagFilters {
            get => _tagFilters;
            set {
                if (_tagFilters == value) {
                    return;
                }
                _tagFilters = value;
                PageConfigurationService.SearchConfiguration.TagFilters = value;
                _includeTagFilterChipModels = [.. value.Select(tf => new ChipModel<TagFilterDTO>() { Value = tf })];
                _excludeTagFilterChipModels = [.. value.Select(tf => new ChipModel<TagFilterDTO>() { Value = tf })];
                value.CollectionChanged += TagFiltersChanged;
                StateHasChanged();
            }
        }

        private void TagFiltersChanged(object? sender, NotifyCollectionChangedEventArgs e) {
            switch (e.Action) {
                case NotifyCollectionChangedAction.Add:
                    List<TagFilterDTO> newTfs = [.. e.NewItems!.Cast<TagFilterDTO>()];
                    List<ChipModel<TagFilterDTO>> newModels = [..  newTfs.Select(tf => new ChipModel<TagFilterDTO>() { Value = tf })];
                    _includeTagFilterChipModels.InsertRange(e.NewStartingIndex, newModels);
                    _excludeTagFilterChipModels.InsertRange(e.NewStartingIndex, newModels);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    // assumes ChipModels has the same order in regards to TagFilterDTO.Id
                    _includeTagFilterChipModels.RemoveRange(e.OldStartingIndex, e.OldItems!.Count);
                    _excludeTagFilterChipModels.RemoveRange(e.OldStartingIndex, e.OldItems!.Count);
                    break;
                default:
                    break;
            }
        }

        private PairedTagFilterSelector _includePairedTagFilterSelector = null!;
        private PairedTagFilterSelector _excludePairedTagFilterSelector = null!;
        private List<ChipModel<TagFilterDTO>> _includeTagFilterChipModels = [];
        private List<ChipModel<TagFilterDTO>> _excludeTagFilterChipModels = [];

        private TagFilterEditor _tagFilterEditor = null!;
        private readonly List<ChipModel<TagDTO>>[] _tagSearchPanelChipModels = [.. TAG_CATEGORIES.Select(t => new List<ChipModel<TagDTO>>())];
        public bool IsAutoSaveEnabled {
            get => PageConfigurationService.SearchConfiguration.IsAutoSaveEnabled;
            set {
                if (PageConfigurationService.SearchConfiguration.IsAutoSaveEnabled = value) {
                    return;
                }
                PageConfigurationService.SearchConfiguration.IsAutoSaveEnabled = value;
                _ = SearchService.UpdateAutoSaveAsync(PageConfigurationService.SearchConfiguration.Id, value);
            }
        }

        public GalleryLanguageDTO SelectedLanguage {
            get => PageConfigurationService.SearchConfiguration.SelectedLanguage;
            set {
                if (PageConfigurationService.SearchConfiguration.SelectedLanguage == value) {
                    return;
                }
                PageConfigurationService.SearchConfiguration.SelectedLanguage = value;
                _ = SearchService.UpdateLanguageAsync(PageConfigurationService.SearchConfiguration.Id, value.Id);
            }
        }
        public GalleryTypeDTO SelectedType {
            get => PageConfigurationService.SearchConfiguration.SelectedType;
            set {
                if (PageConfigurationService.SearchConfiguration.SelectedType == value) {
                    return;
                }
                PageConfigurationService.SearchConfiguration.SelectedType = value;
                _ = SearchService.UpdateTypeAsync(PageConfigurationService.SearchConfiguration.Id, value.Id);
            }
        }

        public string SearchKeywordText {
            get => PageConfigurationService.SearchConfiguration.SearchKeywordText;
            set {
                if (PageConfigurationService.SearchConfiguration.SearchKeywordText == value) {
                    return;
                }
                PageConfigurationService.SearchConfiguration.SearchKeywordText = value;
                _ = SearchService.UpdateSearchKeywordTextAsync(PageConfigurationService.SearchConfiguration.Id, value);
            }
        }

        private ObservableCollection<SearchFilterDTO> _searchFilters = [];
        public ObservableCollection<SearchFilterDTO> SearchFilters {
            get => _searchFilters;
            set {
                if (_searchFilters == value) {
                    return;
                }
                _searchFilters = value;
                PageConfigurationService.SearchConfiguration.SearchFilters = value;
                value.CollectionChanged += SearchFiltersChanged;
            }
        }

        private void SearchFiltersChanged(object? sender, NotifyCollectionChangedEventArgs e) {
            switch (e.Action) {
                case NotifyCollectionChangedAction.Reset:
                    _ = SearchFilterService.ClearAsync(PageConfigurationService.SearchConfiguration.Id);
                    break;
                // Add and Remove are handled in CreateSearchFilter and DeleteSearchFilter
                default:
                    break;
            }
        }

        public SearchPage() {
            Initialized += OnInitRenderComplete;
            Rendered += OnInitRenderComplete;
        }

        private bool _isInitialized = false;
        private bool _isRendered = false;
        private event Action? Initialized;
        private event Action? Rendered;

        protected override async Task OnInitializedAsync() {
            _isInitialized = false;
            _isRendered = false;
            if (!PageConfigurationService.IsSearchConfigurationLoaded) {
                PageConfigurationService.IsSearchConfigurationLoaded = true;
                await Task.WhenAll([
                    GalleryService.GetGalleryLanguagesAsync().ContinueWith(task => PageConfigurationService.Languages = [.. task.Result]),
                    GalleryService.GetGalleryTypesAsync().ContinueWith(task => PageConfigurationService.Types = [.. task.Result]),
                    SearchService.GetConfigurationAsync().ContinueWith(task => PageConfigurationService.SearchConfiguration = task.Result)
                ]);
            }
            TagFilters = [.. PageConfigurationService.SearchConfiguration.TagFilters];
            SearchFilters = [.. PageConfigurationService.SearchConfiguration.SearchFilters];
            _isInitialized = true;
            Initialized?.Invoke();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender) {
            if (firstRender) {
                await JsRuntime.InvokeVoidAsync("setFillHeightResizeObserver", "tag-search-panel-collection", "class", "search-page-left-container", "id");
                _isRendered = true;
                Rendered?.Invoke();
            }
        }

        private void OnInitRenderComplete() {
            if (_isInitialized && _isRendered) {
                _tagFilterEditor.CurrentTagFilter = TagFilters.FirstOrDefault(tf => tf.Id == PageConfigurationService.SearchConfiguration.SelectedTagFilterId);
                foreach (ChipModel<TagFilterDTO> chipModel in _includeTagFilterChipModels) {
                    if (PageConfigurationService.SearchConfiguration.SelectedIncludeTagFilterIds.Contains(chipModel.Value.Id)) {
                        chipModel.Selected = true;
                    }
                }
                foreach (ChipModel<TagFilterDTO> chipModel in _excludeTagFilterChipModels) {
                    if (PageConfigurationService.SearchConfiguration.SelectedExcludeTagFilterIds.Contains(chipModel.Value.Id)) {
                        chipModel.Selected = true;
                    }
                }
                _includePairedTagFilterSelector.SelectedChipModelsChanged += (collection) => {
                    PageConfigurationService.SearchConfiguration.SelectedIncludeTagFilterIds = collection.Select(m => m.Value.Id);
                    _ = SearchService.UpdateIncludeTagFiltersAsync(
                        PageConfigurationService.SearchConfiguration.Id,
                        PageConfigurationService.SearchConfiguration.SelectedIncludeTagFilterIds
                    );
                };
                _excludePairedTagFilterSelector.SelectedChipModelsChanged += (collection) => {
                    PageConfigurationService.SearchConfiguration.SelectedExcludeTagFilterIds = collection.Select(m => m.Value.Id);
                    _ = SearchService.UpdateExcludeTagFiltersAsync(
                        PageConfigurationService.SearchConfiguration.Id,
                        PageConfigurationService.SearchConfiguration.SelectedExcludeTagFilterIds
                    );
                };
            }
        }

        private async Task SelectedTagFilterChanged(ValueChangedEventArgs<TagFilterDTO> args) {
            if (IsAutoSaveEnabled) {
                await SaveTagFilter(args.OldValue);
            }
            if (args.NewValue == null) {
                ClearAllTags();
            } else {
                await LoadTags();
            }
        }

        private void ClearAllTags() {
            for (int i = 0; i < _tagSearchPanelChipModels.Length; i++) {
                _tagSearchPanelChipModels[i].Clear();
            }
        }

        private async Task LoadTags() {
            if (_tagFilterEditor.CurrentTagFilter != null) {
                IEnumerable<TagDTO> tags = await TagFilterService.GetTagsAsync(PageConfigurationService.SearchConfiguration.Id, _tagFilterEditor.CurrentTagFilter.Id);
                if (tags != null) {
                    for (int i = 0; i < TAG_CATEGORIES.Length; i++) {
                        _tagSearchPanelChipModels[i] = [..
                            tags.Where(t => t.Category == TAG_CATEGORIES[i])
                                .OrderBy(t => t.Value.Length)
                                .Select(t => new ChipModel<TagDTO>() { Value = t })
                        ];
                    }
                }
                StateHasChanged();
            }
        }

        private async Task CreateTagFilter() {
            // to prevent enter key acting on the last clicked button
            await JsRuntime.InvokeVoidAsync("document.body.focus");
            DialogParameters<TextFieldDialog> parameters = new() {
                { d => d.ActionText, "Create" }
            };
            IDialogReference dialogRef = await DialogService.ShowAsync<TextFieldDialog>("Create Tag Filter", parameters);
            ((TextFieldDialog)dialogRef.Dialog!).AddValidators(IsDuplicate);
            DialogResult result = (await dialogRef.Result)!;
            if (!result.Canceled) {
                string name = result.Data!.ToString()!;
                TagFilterBuildDTO buildDto = new() {
                    SearchConfigurationId = PageConfigurationService.SearchConfiguration.Id,
                    Name = name,
                    Tags = _tagSearchPanelChipModels.SelectMany(l => l).Select(m => m.Value)
                };
                TagFilterDTO tagFilter = buildDto.ToDTO();
                TagFilters.Add(tagFilter);
                tagFilter.Id = await TagFilterService.CreateAsync(buildDto);
                //if (tagFilter != null) {
                _tagFilterEditor.CurrentTagFilter = tagFilter;
                Snackbar.Add($"Created \"{name}\".", Severity.Success, SNACKBAR_OPTIONS);
                //} else {
                //    Snackbar.Add($"Failed to create \"{name}\".", Severity.Error, SNACKBAR_OPTIONS);
                //}
            }
        }

        private async Task RenameTagFilter() {
            // to prevent enter key acting on the last clicked button
            await JsRuntime.InvokeVoidAsync("document.body.focus");
            string oldName = _tagFilterEditor.CurrentTagFilter!.Name;
            DialogParameters<TextFieldDialog> parameters = new() {
                { d => d.ActionText, "Rename" },
                { d => d.Text, oldName }
            };
            IDialogReference dialogRef = await DialogService.ShowAsync<TextFieldDialog>("Rename Tag Filter", parameters);
            ((TextFieldDialog)dialogRef.Dialog!).AddValidators(IsDuplicate);
            DialogResult result = (await dialogRef.Result)!;
            if (!result.Canceled) {
                string name = result.Data!.ToString()!;
                bool success = await TagFilterService.UpdateNameAsync(
                    PageConfigurationService.SearchConfiguration.Id,
                    _tagFilterEditor.CurrentTagFilter!.Id,
                    name
                );
                if (success) {
                    _tagFilterEditor.CurrentTagFilter.Name = name;
                    Snackbar.Add($"Renamed \"{oldName} to \"{name}\".", Severity.Success, SNACKBAR_OPTIONS);
                } else {
                    Snackbar.Add($"Failed to rename \"{oldName}\".", Severity.Error, SNACKBAR_OPTIONS);
                }
            }
        }

        private string? IsDuplicate(string name) {
            if (TagFilters.Any(tf => tf.Name == name)) {
                return $"\"{name}\" already exists.";
            }
            return null;
        }

        private async Task SaveTagFilter(TagFilterDTO? tagFilter) {
            if (tagFilter != null && TagFilters.Contains(tagFilter) /* tag filter could have been deleted */) {
                bool success = await TagFilterService.UpdateTagsAsync(
                    PageConfigurationService.SearchConfiguration.Id,
                    tagFilter.Id,
                    _tagSearchPanelChipModels.SelectMany(l => l).Select(m => m.Value.Id)
                );
                if (success) {
                    Snackbar.Add($"Saved \"{tagFilter.Name}\"", Severity.Success, SNACKBAR_OPTIONS);
                } else {
                    Snackbar.Add($"Failed to save \"{tagFilter.Name}\"", Severity.Error, SNACKBAR_OPTIONS);
                }
            }
        }

        private async Task DeleteTagFilters() {
            DialogParameters<TagFilterSelectorDialog> parameters = new() {
                { d => d.ActionText, "Delete" },
                { d => d.ChipModels, [.. TagFilters.Select(tf => new ChipModel<TagFilterDTO>() { Value = tf })] }
            };
            IDialogReference dialogRef = await DialogService.ShowAsync<TagFilterSelectorDialog>("Select tag filters to delete", parameters);
            DialogResult result = (await dialogRef.Result)!;
            if (!result.Canceled) {
                IReadOnlyCollection<ChipModel<TagFilterDTO>> selected = (IReadOnlyCollection<ChipModel<TagFilterDTO>>)result.Data!;
                IEnumerable<int> ids = selected.Select(m => m.Value.Id);
                bool success = await TagFilterService.DeleteAsync(PageConfigurationService.SearchConfiguration.Id, ids);
                if (success) {
                    TagFilters = [.. TagFilters.ExceptBy(ids, tf => tf.Id)];
                    Snackbar.Add($"Deleted {selected.Count} tag filters.", Severity.Success, SNACKBAR_OPTIONS);
                    if (_tagFilterEditor.CurrentTagFilter != null && selected.Any(m => m.Value.Id == _tagFilterEditor.CurrentTagFilter.Id)) {
                        _tagFilterEditor.CurrentTagFilter = null;
                    }
                } else {
                    Snackbar.Add($"Failed to delete tag filters.", Severity.Error, SNACKBAR_OPTIONS);
                }
            } 
        }

        private async Task CreateSearchFilter() {
            HashSet<int> includeIds = [.. _includePairedTagFilterSelector.SelectedChipModels.Select(m => m.Value.Id)];
            HashSet<int> excludeIds = [.. _excludePairedTagFilterSelector.SelectedChipModels.Select(m => m.Value.Id)];
            bool currentTagFilterInclude = false;
            bool currentTagFilterExclude = false;
            if (_tagFilterEditor.CurrentTagFilter != null) {
                if (includeIds.Contains(_tagFilterEditor.CurrentTagFilter.Id)) {
                    currentTagFilterInclude = true;
                    includeIds.Remove(_tagFilterEditor.CurrentTagFilter.Id);
                } else if (excludeIds.Contains(_tagFilterEditor.CurrentTagFilter.Id)) {
                    currentTagFilterExclude = true;
                    excludeIds.Remove(_tagFilterEditor.CurrentTagFilter.Id);
                }
            }
            Task<IEnumerable<TagDTO>>? includeTagsTask = null;
            Task<IEnumerable<TagDTO>>? excludeTagsTask = null;
            if (includeIds.Count > 0) {
                includeTagsTask = TagFilterService.GetTagsUnionAsync(PageConfigurationService.SearchConfiguration.Id, includeIds);
            }
            if (excludeIds.Count > 0) {
                excludeTagsTask = TagFilterService.GetTagsUnionAsync(PageConfigurationService.SearchConfiguration.Id, excludeIds);
            }
            await Task.WhenAll(includeTagsTask ?? Task.CompletedTask, excludeTagsTask ?? Task.CompletedTask);
            IEnumerable<TagDTO> includeTagDTOs = includeTagsTask?.Result ?? [];
            IEnumerable<TagDTO> excludeTagDTOs = excludeTagsTask?.Result ?? [];
            IEnumerable<TagDTO> currentTagDTOs = _tagSearchPanelChipModels.SelectMany(l => l).Select(m => m.Value);
            if (currentTagFilterInclude) {
                includeTagDTOs = includeTagDTOs.Union(currentTagDTOs);
            } else if (currentTagFilterExclude) {
                excludeTagDTOs = excludeTagDTOs.Union(currentTagDTOs);
            }
            Dictionary<int, TagDTO> includeDict = includeTagDTOs.ToDictionary(dto => dto.Id);
            HashSet<int> duplicateIds = [.. includeDict.Keys];
            duplicateIds.IntersectWith(excludeTagDTOs.Select(t => t.Id));
            if (duplicateIds.Count > 0) {
                string contentText = string.Join(
                    ", ",
                    duplicateIds.Select(id => includeDict[id])
                                .Select(tag => tag.Category.ToString() + ':' + tag.Value)
                );
                DialogParameters<NotificationDialog> parameters = new() {
                    { d => d.HeaderText, "The following tags are conflicting:" },
                    { d => d.ContentText, contentText },
                };
                await DialogService.ShowAsync<NotificationDialog>("Duplicate Tags", parameters);
                return;
            }

            SearchFilterDTOBuilder builder = new() {
                Language = SelectedLanguage,
                Type = SelectedType,
                SearchKeywordText = SearchKeywordText,
                IncludeTags = includeTagDTOs,
                ExcludeTags = excludeTagDTOs
            };
            SearchFilterDTO dto = builder.Build();
            SearchFilters.Add(dto);
            await JsRuntime.InvokeVoidAsync("navigator.clipboard.writeText", dto.SearchLink);
            int id = await SearchFilterService.CreateAsync(PageConfigurationService.SearchConfiguration.Id, dto);
            //if (success) {
            dto.Id = id;
            //} else {
            //    Snackbar.Add("Failed to create search filter.", Severity.Error, SNACKBAR_OPTIONS);
            //}
        }

        private async Task DeleteSearchFilter(SearchFilterDTO dto) {
            bool success = await SearchFilterService.DeleteAsync(PageConfigurationService.SearchConfiguration.Id, dto.Id);
            if (success) {
                SearchFilters.Remove(dto);
            } else {
                Snackbar.Add("Failed to delete search filter.", Severity.Error, SNACKBAR_OPTIONS);
            }
        }
    }
}
