using HitomiScrollViewerWebApp.Models;
using Microsoft.AspNetCore.Components;

namespace HitomiScrollViewerWebApp.Components {
    public partial class SearchFilter : ComponentBase {
        [Parameter, EditorRequired] public SearchFilterModel Model { get; set; } = null!;
    }
}
