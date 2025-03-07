using HitomiScrollViewerData;
using HitomiScrollViewerData.DTOs;
using HitomiScrollViewerData.Entities;
using HitomiScrollViewerWebApp.Components;
using HitomiScrollViewerWebApp.Models;
using MudBlazor;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Security.AccessControl;

namespace HitomiScrollViewerWebApp.Pages {
    public partial class Search {
        private static readonly TagCategory[] TAG_CATEGORIES = Enum.GetValues<TagCategory>();
        private const string BORDER_SOLID_PRIMARY = "1px solid var(--mud-palette-primary)";
        private static readonly Action<SnackbarOptions> SNACKBAR_OPTIONS = (options => {
            options.ShowCloseIcon = true;
            options.CloseAfterNavigation = true;
            options.ShowTransitionDuration = 0;
            options.HideTransitionDuration = 500;
            options.VisibleStateDuration = 3000;
            options.DuplicatesBehavior = SnackbarDuplicatesBehavior.Allow;
        });

        private ObservableCollection<TagFilterDTO> _tagFilters = [];
        public ObservableCollection<TagFilterDTO> TagFilters {
            get => _tagFilters;
            set {
                _tagFilters = value;
                _tagFilterEditor.TagFilters = [.. value];
                _includeTagFilterChipModels = [.. value.Select(tf => new ChipModel<TagFilterDTO>() { Value = tf })];
                _excludeTagFilterChipModels = [.. value.Select(tf => new ChipModel<TagFilterDTO>() { Value = tf })];
                value.CollectionChanged += TagFiltersChanged;
            }
        }

        private void TagFiltersChanged(object? sender, NotifyCollectionChangedEventArgs e) {
            switch (e.Action) {
                case NotifyCollectionChangedAction.Add:
                    List<TagFilterDTO> newTfs = [.. e.NewItems!.Cast<TagFilterDTO>()];
                    List<ChipModel<TagFilterDTO>> newModels = [..  newTfs.Select(tf => new ChipModel<TagFilterDTO>() { Value = tf })];
                    _tagFilterEditor.TagFilters.InsertRange(e.NewStartingIndex, newTfs);
                    _includeTagFilterChipModels.InsertRange(e.NewStartingIndex, newModels);
                    _excludeTagFilterChipModels.InsertRange(e.NewStartingIndex, newModels);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    _tagFilterEditor.TagFilters.RemoveRange(e.OldStartingIndex, e.OldItems!.Count);
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
        private bool _isAutoSaveEnabled = true;

        private List<GalleryLanguageDTO> _galleryLanguages = [];
        private List<GalleryTypeDTO> _galleryTypes = [];

        public Search() {
            _tagSearchChipSetModels =
            [..
                TAG_CATEGORIES.Select(tagCategory => new TagSearchChipSetModel() {
                        TagCategory = tagCategory,
                        Label = tagCategory.ToString(),
                        ToStringFunc = (tag => tag.Value),
                        SearchFunc = async (string text, CancellationToken ct) => {
                            List<Tag>? tags = await TagService.GetTagsAsync(tagCategory, 8, text, ct);
                            return tags == null ? [] : tags.Select(tag => tag.ToTagDTO());
                        }
                    }
                )
            ];
        }

        protected override async Task OnInitializedAsync() {
            TagFilters = [.. await TagFilterService.GetTagFiltersAsync()];
            _galleryLanguages = await GalleryService.GetGalleryLanguages();
            _galleryTypes = await GalleryService.GetGalleryTypes();
        }

        protected override void OnAfterRender(bool firstRender) {
            if (firstRender) {
                _includePairedTagFilterSelector.Other = _excludePairedTagFilterSelector;
                _excludePairedTagFilterSelector.Other = _includePairedTagFilterSelector;
            }
        }

        private async Task SelectedTagFilterChanged(ValueChangedEventArgs<TagFilterDTO> args) {
            if (_isAutoSaveEnabled) {
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
                List<TagDTO>? tags = await TagService.GetTagsFromTagFilter(_tagFilterEditor.CurrentTagFilter.Id);
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
                TagFilterDTO? tagFilter = await TagFilterService.CreateTagFilterAsync(
                    name,
                    _tagSearchChipSetModels.SelectMany(m => m.ChipModels).Select(m => m.Value)
                );
                if (tagFilter != null) {
                    TagFilters.Add(tagFilter);
                    _tagFilterEditor.CurrentTagFilter = tagFilter;
                    Snackbar.Add($"Created \"{name}\".", Severity.Success, SNACKBAR_OPTIONS);
                } else {
                    Snackbar.Add($"Failed to create \"{name}\".", Severity.Error, SNACKBAR_OPTIONS);
                }
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
                bool success = await TagFilterService.UpdateTagFilterAsync(_tagFilterEditor.CurrentTagFilter!.Id, name);
                if (success) {
                    _tagFilterEditor.CurrentTagFilter.Name = name;
                    Snackbar.Add($"Renamed \"{oldName} to \"{name}\".", Severity.Success, SNACKBAR_OPTIONS);
                } else {
                    Snackbar.Add($"Failed to rename \"{oldName}\".", Severity.Error, SNACKBAR_OPTIONS);
                }
            }
        }

        private async Task<string?> IsDuplicate(string name) {
            IEnumerable<TagFilterDTO>? tagFilters = await TagFilterService.GetTagFiltersAsync();
            if (tagFilters == null) {
                return "An error has occurred while while fetching tag filters.";
            }
            if (tagFilters.Any(tf => tf.Name == name)) {
                return $"\"{name}\" already exists.";
            }
            return null;
        }

        private async Task SaveTagFilter(TagFilterDTO? tagFilter) {
            if (tagFilter != null && TagFilters.Contains(tagFilter) /* tag filter could have been deleted */) {
                bool success = await TagFilterService.UpdateTagFilterAsync(
                    tagFilter.Id,
                    _tagSearchChipSetModels.SelectMany(m => m.ChipModels).Select(m => m.Value)
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
                bool success = await TagFilterService.DeleteTagFiltersAsync(ids);
                if (success) {
                    TagFilters = [.. TagFilters.ExceptBy(ids, tf => tf.Id)];
                    if (_tagFilterEditor.CurrentTagFilter != null && selected.Any(m => m.Value.Id == _tagFilterEditor.CurrentTagFilter.Id)) {
                        _tagFilterEditor.CurrentTagFilter = null;
                    }
                    Snackbar.Add($"Deleted {selected.Count} tag filters.", Severity.Success, SNACKBAR_OPTIONS);
                } else {
                    Snackbar.Add($"Failed to delete tag filters.", Severity.Error, SNACKBAR_OPTIONS);
                }
            } 
        }
    }
}
