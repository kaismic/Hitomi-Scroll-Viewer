using HitomiScrollViewerData;
using HitomiScrollViewerData.DTOs;
using HitomiScrollViewerData.Entities;
using HitomiScrollViewerWebApp.Components;
using HitomiScrollViewerWebApp.Models;
using MudBlazor;

namespace HitomiScrollViewerWebApp.Pages {
    public partial class Search {
        private static readonly TagCategory[] TAG_CATEGORIES = Enum.GetValues<TagCategory>();
        private const string BORDER_SOLID_PRIMARY = "1px solid var(--mud-palette-primary)";
        private static readonly Action<SnackbarOptions> SNACKBAR_OPTIONS = (options =>
        {
            options.ShowCloseIcon = true;
            options.CloseAfterNavigation = true;
            options.ShowTransitionDuration = 0;
            options.HideTransitionDuration = 500;
            options.VisibleStateDuration = 3000;
            options.DuplicatesBehavior = SnackbarDuplicatesBehavior.Allow;
        });

        // TODO bind with TagFilterEditor and TagFilterSelector
        private List<TagFilterDTO> TagFilters = [];

        private TagFilterEditor _tagFilterEditor = null!;
        private TagSearchChipSetModel[] _tagSearchChipSetModels = new TagSearchChipSetModel[TAG_CATEGORIES.Length];
        private bool _isAutoSaveEnabled = true;

        public Search() {
            _tagSearchChipSetModels = [.. TAG_CATEGORIES.Select(tagCategory => new TagSearchChipSetModel() {
            TagCategory = tagCategory,
            Label = tagCategory.ToString(),
            ToStringFunc = (tag => tag.Value),
            SearchFunc = async (string text, CancellationToken ct) => {
                List<Tag>? tags = await TagService.GetTagsAsync(tagCategory, 8, text, ct);
                return tags == null ? [] : tags.Select(tag => tag.ToTagDTO());
            }
        })];
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
            var parameters = new DialogParameters<TextInputDialog> {
                { d => d.ActionText, "Create" },
                { d => d.Validators, [IsDuplicate] }
            };
            IDialogReference dialog = await DialogService.ShowAsync<TextInputDialog>("Create Tag Filter", parameters);
            DialogResult result = (await dialog.Result)!;
            if (!result.Canceled) {
                string name = result.Data!.ToString()!;
                TagFilterDTO? tagFilter = await TagFilterService.CreateTagFilterAsync(
                    name,
                    _tagSearchChipSetModels.SelectMany(m => m.ChipModels).Select(m => m.Value)
                );
                if (tagFilter != null) {
                    _tagFilterEditor.TagFilters.Add(tagFilter);
                    _tagFilterEditor.CurrentTagFilter = tagFilter;
                    Snackbar.Add($"Created \"{name}\".", Severity.Success, SNACKBAR_OPTIONS);
                } else {
                    Snackbar.Add($"Failed to create \"{name}\".", Severity.Error, SNACKBAR_OPTIONS);
                }
            }
        }

        private async Task RenameTagFilter() {
            var parameters = new DialogParameters<TextInputDialog> {
                { d => d.ActionText, "Rename" },
                { d => d.Validators, [IsDuplicate] }
            };
            IDialogReference dialog = await DialogService.ShowAsync<TextInputDialog>("Rename Tag Filter", parameters);
            DialogResult result = (await dialog.Result)!;
            if (!result.Canceled) {
                string oldName = _tagFilterEditor.CurrentTagFilter!.Name;
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
            if (tagFilter != null) {
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
    }
}
