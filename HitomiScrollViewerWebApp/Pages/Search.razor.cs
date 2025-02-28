using HitomiScrollViewerData.DTOs;
using HitomiScrollViewerData;
using HitomiScrollViewerWebApp.Models;
using MudBlazor;
using HitomiScrollViewerWebApp.Components;
using HitomiScrollViewerData.Entities;

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

        private async Task SelectedTagFilterChanged(ValueChangedEventArgs<TagFilter> args) {
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
                    .Select(t => new SearchChipModel<TagDTO>() { Value = t })
                        ];
                    }
                }
                StateHasChanged();
            }
        }

        private async Task SaveTagFilter(TagFilter? tagFilter) {
            if (tagFilter != null) {
                HttpResponseMessage response = await TagFilterService.UpdateTagFilterAsync(
                    tagFilter.Id,
                    _tagSearchChipSetModels.SelectMany(m => m.ChipModels).Select(m => m.Value)
                );
                if (response.IsSuccessStatusCode) {
                    Snackbar.Add($"Saved {tagFilter.Name}", Severity.Success, SNACKBAR_OPTIONS);
                } else {
                    Snackbar.Add($"Failed to save {tagFilter.Name}", Severity.Error, SNACKBAR_OPTIONS);
                }
            }
        }
    }
}
