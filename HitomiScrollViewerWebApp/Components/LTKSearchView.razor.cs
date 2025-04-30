using HitomiScrollViewerData.DTOs;
using HitomiScrollViewerWebApp.Services;
using Microsoft.AspNetCore.Components;

namespace HitomiScrollViewerWebApp.Components {
    public partial class LTKSearchView : ComponentBase {
        [Inject] private LanguageTypeService LanguageTypeService { get; set; } = default!;
        [Parameter] public string? Style { get; set; }
        [Parameter] public string? Class { get; set; }
        [Parameter, EditorRequired] public GalleryLanguageDTO SelectedLanguage { get; set; } = default!;
        [Parameter] public EventCallback<GalleryLanguageDTO> SelectedLanguageChanged { get; set; }
        [Parameter, EditorRequired] public GalleryTypeDTO SelectedType { get; set; } = default!;
        [Parameter] public EventCallback<GalleryTypeDTO> SelectedTypeChanged { get; set; }
        [Parameter, EditorRequired] public string TitleSearchKeyword { get; set; } = "";
        [Parameter] public EventCallback<string> TitleSearchKeywordChanged { get; set; }
    }
}