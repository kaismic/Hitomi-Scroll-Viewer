using HitomiScrollViewerData.DTOs;
using Microsoft.AspNetCore.Components;

#pragma warning disable BL0007 // Component parameters should be auto properties
namespace HitomiScrollViewerWebApp.Components {
    public partial class LTKSearchView : ComponentBase {
        private GalleryLanguageDTO _selectedLanguage = default!;
        [Parameter, EditorRequired]
        public GalleryLanguageDTO SelectedLanguage {
            get => _selectedLanguage;
            set {
                if (_selectedLanguage == value) {
                    return;
                }
                _selectedLanguage = value;
                SelectedLanguageChanged.InvokeAsync(value);
            }
        }
        [Parameter] public EventCallback<GalleryLanguageDTO> SelectedLanguageChanged { get; set; }
        
        private GalleryTypeDTO _selectedType = default!;
        [Parameter, EditorRequired]
        public GalleryTypeDTO SelectedType {
            get => _selectedType;
            set {
                if (_selectedType == value) {
                    return;
                }
                _selectedType = value;
                SelectedTypeChanged.InvokeAsync(value);
            }
        }
        [Parameter] public EventCallback<GalleryTypeDTO> SelectedTypeChanged { get; set; }

        private string _searchKeywordText = "";
        [Parameter, EditorRequired]
        public string SearchKeywordText {
            get => _searchKeywordText;
            set {
                if (_searchKeywordText == value) {
                    return;
                }
                _searchKeywordText = value;
                SearchKeywordTextChanged.InvokeAsync(value);
            }
        }
        [Parameter] public EventCallback<string> SearchKeywordTextChanged { get; set; }
    }
}
#pragma warning restore BL0007 // Component parameters should be auto properties