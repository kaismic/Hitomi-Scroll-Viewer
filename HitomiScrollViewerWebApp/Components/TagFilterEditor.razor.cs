using HitomiScrollViewerData;
using HitomiScrollViewerData.Entities;
using HitomiScrollViewerWebApp.Services;
using Microsoft.AspNetCore.Components;

namespace HitomiScrollViewerWebApp.Components {
    public partial class TagFilterEditor : ComponentBase {
        [Inject] private TagFilterService TagFilterService { get; set; } = null!;

        [Parameter, EditorRequired] public EventCallback<ValueChangedEventArgs<TagFilter>> SelectedTagFilterChanged { get; set; }
        [Parameter, EditorRequired] public EventCallback OnCreateButtonClicked { get; set; }
        [Parameter, EditorRequired] public EventCallback OnRenameButtonClicked { get; set; }
        [Parameter, EditorRequired] public EventCallback OnSaveButtonClicked { get; set; }
        [Parameter, EditorRequired] public EventCallback OnDeleteButtonClicked { get; set; }

        public List<TagFilter> TagFilters = [];
        private TagFilter? _oldTagFilter;
        public TagFilter? CurrentTagFilter;

        protected override async Task OnInitializedAsync() {
            TagFilters = await TagFilterService.GetTagFiltersAsync() ?? [];
            await base.OnInitializedAsync();
        }

        private void SelectedTagFiltersChangedInternal(IEnumerable<TagFilter> tagFilters) {
            CurrentTagFilter = tagFilters.FirstOrDefault();
            SelectedTagFilterChanged.InvokeAsync(new(_oldTagFilter, CurrentTagFilter));
            _oldTagFilter = CurrentTagFilter;
        }
    }
}
