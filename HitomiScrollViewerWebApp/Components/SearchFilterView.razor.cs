using HitomiScrollViewerData.DTOs;
using Microsoft.AspNetCore.Components;

namespace HitomiScrollViewerWebApp.Components {
    public partial class SearchFilterView : ComponentBase {
        [Parameter, EditorRequired] public SearchFilterDTO Model { get; set; } = default!;
    }
}
