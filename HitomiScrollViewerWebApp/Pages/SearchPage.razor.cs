using HitomiScrollViewerData;
using HitomiScrollViewerData.Builders;
using HitomiScrollViewerData.DTOs;
using HitomiScrollViewerData.Entities;
using HitomiScrollViewerWebApp.Components;
using HitomiScrollViewerWebApp.Models;
using HitomiScrollViewerWebApp.Services;
using Microsoft.JSInterop;
using MudBlazor;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using static HitomiScrollViewerData.Entities.Tag;

namespace HitomiScrollViewerWebApp.Pages {
    public partial class SearchPage {
        private const string JAVASCRIPT_FILE = $"./Pages/{nameof(SearchPage)}.razor.js";

        private static readonly Action<SnackbarOptions> SNACKBAR_OPTIONS = options => {
            options.ShowCloseIcon = true;
            options.CloseAfterNavigation = true;
            options.ShowTransitionDuration = 0;
            options.HideTransitionDuration = 500;
            options.VisibleStateDuration = 3000;
            options.DuplicatesBehavior = SnackbarDuplicatesBehavior.Allow;
        };

        private IJSObjectReference? _jsModule;

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
        private readonly TagSearchChipSetModel[] _tagSearchChipSetModels = new TagSearchChipSetModel[TAG_CATEGORIES.Length];
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

        public string SearchKeywordText {
            get => PageConfigurationService.SearchConfiguration.SearchKeywordText;
            set {
                PageConfigurationService.SearchConfiguration.SearchKeywordText = value;
                _ = SearchService.UpdateSearchKeywordTextAsync(PageConfigurationService.SearchConfiguration.Id, value);
            }
        }

        public SearchPage() {
            _tagSearchChipSetModels =
            [..
                TAG_CATEGORIES.Select(tagCategory => new TagSearchChipSetModel() {
                        TagCategory = tagCategory,
                        Label = tagCategory.ToString(),
                        ToStringFunc = tag => tag.Value,
                        SearchFunc = async (string text, CancellationToken ct) => {
                            IEnumerable<Tag> tags = await TagService.GetTagsAsync(tagCategory, 8, text, ct);
                            return tags.Select(tag => tag.ToDTO());
                        }
                    }
                )
            ];
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
#pragma warning disable CA2012 // Use ValueTasks correctly
                _jsModule ??= await JsRuntime.InvokeAsync<IJSObjectReference>("import", JAVASCRIPT_FILE);
                _ = _jsModule.InvokeVoidAsync("setChipSetContainerHeight");
#pragma warning restore CA2012 // Use ValueTasks correctly
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
            foreach (TagSearchChipSetModel model in _tagSearchChipSetModels) {
                model.ChipModels = [];
            }
        }

        private async Task LoadTags() {
            if (_tagFilterEditor.CurrentTagFilter != null) {
                IEnumerable<TagDTO> tags = await TagFilterService.GetTagsAsync(PageConfigurationService.SearchConfiguration.Id, _tagFilterEditor.CurrentTagFilter.Id);
                if (tags != null) {
                    foreach (TagSearchChipSetModel model in _tagSearchChipSetModels) {
                        model.ChipModels = [..
                            tags.Where(t => t.Category == model.TagCategory)
                                .Select(t => new ChipModel<TagDTO>() { Value = t })
                        ];
                    }
                }
                StateHasChanged();
            }
        }

        private async Task CreateTagFilter() {
            DialogTextField dialogContent = null!;
            var parameters = new DialogParameters<TagFilterEditDialog> {
                { d => d.ActionText, "Create" },
                { d => d.DialogContent,
                    builder => {
                        builder.OpenComponent<DialogTextField>(0);
                        builder.AddComponentReferenceCapture(1, (component) => {
                            dialogContent = (DialogTextField)component;
#pragma warning disable BL0005 // Component parameter should not be set outside of its component.
                            dialogContent.Validators = [IsDuplicate];
#pragma warning restore BL0005 // Component parameter should not be set outside of its component.
                        });
                        builder.CloseComponent();
                    }
                },
            };
            IDialogReference dialog = await DialogService.ShowAsync<TagFilterEditDialog>("Create Tag Filter", parameters);
            ((TagFilterEditDialog)dialog.Dialog!).DialogContentRef = dialogContent;
            DialogResult result = (await dialog.Result)!;
            if (!result.Canceled) {
                string name = result.Data!.ToString()!;
                TagFilterBuildDTO buildDto = new() {
                    SearchConfigurationId = PageConfigurationService.SearchConfiguration.Id,
                    Name = name,
                    Tags = _tagSearchChipSetModels.SelectMany(m => m.ChipModels).Select(m => m.Value)
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
            string oldName = _tagFilterEditor.CurrentTagFilter!.Name;
            DialogTextField dialogContent = null!;
            var parameters = new DialogParameters<TagFilterEditDialog> {
                { d => d.ActionText, "Rename" },
                { d => d.DialogContent,
                    builder => {
                        builder.OpenComponent<DialogTextField>(0);
                        builder.AddComponentReferenceCapture(1, (component) => {
                            dialogContent = (DialogTextField)component;
#pragma warning disable BL0005 // Component parameter should not be set outside of its component.
                            dialogContent.Validators = [IsDuplicate];
                            dialogContent.Text = oldName;
#pragma warning restore BL0005 // Component parameter should not be set outside of its component.
                        });
                        builder.CloseComponent();
                    }
                },
            };
            IDialogReference dialog = await DialogService.ShowAsync<TagFilterEditDialog>("Rename Tag Filter", parameters);
            ((TagFilterEditDialog)dialog.Dialog!).DialogContentRef = dialogContent;
            DialogResult result = (await dialog.Result)!;
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
            //IEnumerable<TagFilterDTO>? tagFilters = await TagFilterService.GetTagFiltersAsync();
            //if (tagFilters == null) {
            //    return "An error has occurred while while fetching tag filters.";
            //}
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
                    _tagSearchChipSetModels.SelectMany(m => m.ChipModels).Select(m => m.Value.Id)
                );
                if (success) {
                    Snackbar.Add($"Saved \"{tagFilter.Name}\"", Severity.Success, SNACKBAR_OPTIONS);
                } else {
                    Snackbar.Add($"Failed to save \"{tagFilter.Name}\"", Severity.Error, SNACKBAR_OPTIONS);
                }
            }
        }

        private async Task DeleteTagFilters() {
            DialogTagFilterSelector dialogContent = null!;
            var parameters = new DialogParameters<TagFilterEditDialog> {
                { d => d.ActionText, "Delete" },
                { d => d.DialogContent,
                    builder => {
                        builder.OpenComponent<DialogTagFilterSelector>(0);
                        builder.AddComponentReferenceCapture(1, (component) => {
                            dialogContent = (DialogTagFilterSelector)component;
#pragma warning disable BL0005 // Component parameter should not be set outside of its component.
                            dialogContent.ChipModels = [.. TagFilters.Select(tf => new ChipModel<TagFilterDTO>() { Value = tf })];
#pragma warning restore BL0005 // Component parameter should not be set outside of its component.
                        });
                        builder.CloseComponent();
                    }
                },
            };
            IDialogReference dialog = await DialogService.ShowAsync<TagFilterEditDialog>("Select tag filters to delete", parameters);
            ((TagFilterEditDialog)dialog.Dialog!).DialogContentRef = dialogContent;
            DialogResult result = (await dialog.Result)!;
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
            IEnumerable<TagDTO> currentTagDTOs = _tagSearchChipSetModels.SelectMany(m => m.ChipModels).Select(m => m.Value);
            if (currentTagFilterInclude) {
                includeTagDTOs = includeTagDTOs.Union(currentTagDTOs);
            } else if (currentTagFilterExclude) {
                excludeTagDTOs = excludeTagDTOs.Union(currentTagDTOs);
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
