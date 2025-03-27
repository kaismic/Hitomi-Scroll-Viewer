using HitomiScrollViewerData.DTOs;
using Microsoft.AspNetCore.Components;

namespace HitomiScrollViewerWebApp.Components {
    public partial class LTKSearchView : ComponentBase {
        private GalleryLanguageDTO _selectedLanguage = default!;
        [Parameter, EditorRequired] public GalleryLanguageDTO SelectedLanguage { get; set; } = default!;
        [Parameter] public EventCallback<GalleryLanguageDTO> SelectedLanguageChanged { get; set; }
        private GalleryTypeDTO _selectedType = default!;
        [Parameter, EditorRequired] public GalleryTypeDTO SelectedType { get; set; } = default!;
        [Parameter] public EventCallback<GalleryTypeDTO> SelectedTypeChanged { get; set; }
        private string? _searchKeywordText;
        [Parameter, EditorRequired] public string SearchKeywordText { get; set; } = "";
        [Parameter] public EventCallback<string> SearchKeywordTextChanged { get; set; }

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
            if (_searchKeywordText != SearchKeywordText) {
                if (_searchKeywordText != null) {
                    await SearchKeywordTextChanged.InvokeAsync(SearchKeywordText);
                }
                _searchKeywordText = SearchKeywordText;
            }
        }
    }
}