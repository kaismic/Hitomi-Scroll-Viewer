using HitomiScrollViewerData.DTOs;
using System.Net.Http.Json;

namespace HitomiScrollViewerWebApp.Services {
    public class LanguageTypeService(HttpClient httpClient) {
        public async Task<IEnumerable<GalleryLanguageDTO>> GetLanguagesAsync() {
            return (await httpClient.GetFromJsonAsync<IEnumerable<GalleryLanguageDTO>>("api/language-type/languages"))!;
        }

        public async Task<IEnumerable<GalleryTypeDTO>> GetTypesAsync() {
            return (await httpClient.GetFromJsonAsync<IEnumerable<GalleryTypeDTO>>("api/language-type/types"))!;
        }
    }
}
