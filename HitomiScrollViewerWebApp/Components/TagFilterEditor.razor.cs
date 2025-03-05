using HitomiScrollViewerData;
using HitomiScrollViewerData.DTOs;
using Microsoft.AspNetCore.Components;

namespace HitomiScrollViewerWebApp.Components {
    public partial class TagFilterEditor : ComponentBase {
        [Parameter, EditorRequired] public List<TagFilterDTO> TagFilters { get; set; } = [];
        [Parameter, EditorRequired] public EventCallback<ValueChangedEventArgs<TagFilterDTO>> SelectedTagFilterChanged { get; set; }
        [Parameter, EditorRequired] public EventCallback OnCreateButtonClicked { get; set; }
        [Parameter, EditorRequired] public EventCallback OnRenameButtonClicked { get; set; }
        [Parameter, EditorRequired] public EventCallback OnSaveButtonClicked { get; set; }
        [Parameter, EditorRequired] public EventCallback OnDeleteButtonClicked { get; set; }
        private TagFilterDTO? _currentTagFilter;
        public TagFilterDTO? CurrentTagFilter {
            get => _currentTagFilter;
            set {
                TagFilterDTO? oldValue = _currentTagFilter;
                _currentTagFilter = value;
                SelectedTagFilterChanged.InvokeAsync(new(oldValue, value));
            }
        }
    }
}
