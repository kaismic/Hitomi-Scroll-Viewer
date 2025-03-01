using HitomiScrollViewerData;
using HitomiScrollViewerData.DTOs;
using HitomiScrollViewerData.Entities;
using HitomiScrollViewerWebApp.Services;
using Microsoft.AspNetCore.Components;

namespace HitomiScrollViewerWebApp.Components {
    public partial class TagFilterEditor : ComponentBase {
        [Inject] private TagFilterService TagFilterService { get; set; } = null!;

        [Parameter, EditorRequired] public EventCallback<ValueChangedEventArgs<TagFilterDTO>> SelectedTagFilterChanged { get; set; }
        [Parameter, EditorRequired] public EventCallback OnCreateButtonClicked { get; set; }
        [Parameter, EditorRequired] public EventCallback OnRenameButtonClicked { get; set; }
        [Parameter, EditorRequired] public EventCallback OnSaveButtonClicked { get; set; }
        [Parameter, EditorRequired] public EventCallback OnDeleteButtonClicked { get; set; }

        public List<TagFilterDTO> TagFilters = [];
        private TagFilterDTO? _currentTagFilter;
        public TagFilterDTO? CurrentTagFilter {
            get => _currentTagFilter;
            set {
                TagFilterDTO? oldValue = _currentTagFilter;
                _currentTagFilter = value;
                SelectedTagFilterChanged.InvokeAsync(new(oldValue, value));
            }
        }

        protected override async Task OnInitializedAsync() {
            IEnumerable<TagFilterDTO>? result = await TagFilterService.GetTagFiltersAsync();
            TagFilters = result == null ? [] : [.. result];
            await base.OnInitializedAsync();
        }
    }
}
