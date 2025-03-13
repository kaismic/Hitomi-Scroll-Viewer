using HitomiScrollViewerData;
using HitomiScrollViewerData.DTOs;
using Microsoft.AspNetCore.Components;

namespace HitomiScrollViewerWebApp.Components {
    public partial class TagFilterEditor : ComponentBase {
        public List<TagFilterDTO> TagFilters { get; set; } = [];
        public int SearchQueryConfigId { get; set; }
        [Parameter, EditorRequired] public EventCallback OnCreateButtonClicked { get; set; }
        [Parameter, EditorRequired] public EventCallback OnRenameButtonClicked { get; set; }
        [Parameter, EditorRequired] public EventCallback OnSaveButtonClicked { get; set; }
        [Parameter, EditorRequired] public EventCallback OnDeleteButtonClicked { get; set; }
        [Parameter] public EventCallback<ValueChangedEventArgs<TagFilterDTO>> SelectedTagFilterChanged { get; set; }
        private TagFilterDTO? _currentTagFilter;
        public TagFilterDTO? CurrentTagFilter {
            get => _currentTagFilter;
            set {
                if (value == _currentTagFilter) {
                    return;
                }
                TagFilterDTO? oldValue = _currentTagFilter;
                _currentTagFilter = value;
                _ = QueryConfigurationService.UpdateSearchSelectedTagFilterAsync(SearchQueryConfigId, value?.Id ?? 0);
                SelectedTagFilterChanged.InvokeAsync(new(oldValue, value));
            }
        }
    }
}