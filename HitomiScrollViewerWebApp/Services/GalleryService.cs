using HitomiScrollViewerData.DTOs;
using System.Net.Http.Json;

namespace HitomiScrollViewerWebApp.Services {
    public class GalleryService(HttpClient httpClient) {
        public async Task<IEnumerable<GalleryLanguageDTO>> GetGalleryLanguagesAsync() {
            return (await httpClient.GetFromJsonAsync<IEnumerable<GalleryLanguageDTO>>("api/gallery/languages"))!;
        }

        public async Task<IEnumerable<GalleryTypeDTO>> GetGalleryTypesAsync() {
            return (await httpClient.GetFromJsonAsync<IEnumerable<GalleryTypeDTO>>("api/gallery/types"))!;
        }
    }
}
