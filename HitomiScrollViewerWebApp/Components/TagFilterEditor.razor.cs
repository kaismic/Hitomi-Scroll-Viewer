using HitomiScrollViewerData;
using HitomiScrollViewerData.DTOs;
using HitomiScrollViewerWebApp.Services;
using Microsoft.AspNetCore.Components;

namespace HitomiScrollViewerWebApp.Components {
    public partial class TagFilterEditor : ComponentBase {
        [Parameter, EditorRequired] public IEnumerable<TagFilterDTO> TagFilters { get; set; } = null!;
        [Parameter, EditorRequired] public EventCallback OnCreateButtonClicked { get; set; }
        [Parameter, EditorRequired] public EventCallback OnRenameButtonClicked { get; set; }
        [Parameter, EditorRequired] public EventCallback OnSaveButtonClicked { get; set; }
        [Parameter, EditorRequired] public EventCallback OnDeleteButtonClicked { get; set; }
        [Parameter] public EventCallback<ValueChangedEventArgs<TagFilterDTO>> SelectedTagFilterChanged { get; set; }
        private bool _isFirstCurrentTagFilterSet = true;
        private TagFilterDTO? _currentTagFilter;
        public TagFilterDTO? CurrentTagFilter {
            get => _currentTagFilter;
            set {
                if (_currentTagFilter == value) {
                    return;
                }
                TagFilterDTO? oldValue = _currentTagFilter;
                _currentTagFilter = value;
                PageConfigurationService.SearchConfiguration.SelectedTagFilterId = value?.Id ?? 0;
                if (_isFirstCurrentTagFilterSet) {
                    _isFirstCurrentTagFilterSet = false;
                } else {
                    _ = SearchService.UpdateSelectedTagFilterAsync(
                        PageConfigurationService.SearchConfiguration.Id,
                        PageConfigurationService.SearchConfiguration.SelectedTagFilterId
                    );
                }
                SelectedTagFilterChanged.InvokeAsync(new(oldValue, value));
            }
        }
    }
}