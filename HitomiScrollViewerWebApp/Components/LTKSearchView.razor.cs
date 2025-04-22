using HitomiScrollViewerData.DTOs;
using HitomiScrollViewerWebApp.Services;
using Microsoft.AspNetCore.Components;

namespace HitomiScrollViewerWebApp.Components {
    public partial class LTKSearchView : ComponentBase {
        [Inject] private LanguageTypeService LanguageTypeService { get; set; } = default!;
        [Parameter] public string? Style { get; set; }
        [Parameter] public string? Class { get; set; }

        private GalleryLanguageDTO _selectedLanguage = default!;
        [Parameter, EditorRequired] public GalleryLanguageDTO SelectedLanguage { get; set; } = default!;
        [Parameter] public EventCallback<GalleryLanguageDTO> SelectedLanguageChanged { get; set; }
        private GalleryTypeDTO _selectedType = default!;
        [Parameter, EditorRequired] public GalleryTypeDTO SelectedType { get; set; } = default!;
        [Parameter] public EventCallback<GalleryTypeDTO> SelectedTypeChanged { get; set; }
        private string? _titleSearchKeyword;
        [Parameter, EditorRequired] public string TitleSearchKeyword { get; set; } = "";
        [Parameter] public EventCallback<string> TitleSearchKeywordChanged { get; set; }

        protected override async Task OnParametersSetAsync() {
            if (_selectedLanguage != SelectedLanguage) {
                if (_selectedLanguage != null) {
                    await SelectedLanguageChanged.InvokeAsync(SelectedLanguage);
                }
                _selectedLanguage = SelectedLanguage;
            }
            if (_selectedType != SelectedType) {
                if (_selectedType != null) {
                    await SelectedTypeChanged.InvokeAsync(SelectedType);
                }
                _selectedType = SelectedType;
            }
            if (_titleSearchKeyword != TitleSearchKeyword) {
                if (_titleSearchKeyword != null) {
                    await TitleSearchKeywordChanged.InvokeAsync(TitleSearchKeyword);
                }
                _titleSearchKeyword = TitleSearchKeyword;
            }
        }
    }
}