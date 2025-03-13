using HitomiScrollViewerData.DTOs;
using System.Net.Http.Json;

namespace HitomiScrollViewerWebApp.Services {
    public class GalleryService(HttpClient httpClient) {
        public async Task<List<GalleryLanguageDTO>> GetGalleryLanguagesAsync() {
            return (await httpClient.GetFromJsonAsync<List<GalleryLanguageDTO>>("api/gallery/languages"))!;
        }

        public async Task<List<GalleryTypeDTO>> GetGalleryTypesAsync() {
            return (await httpClient.GetFromJsonAsync<List<GalleryTypeDTO>>("api/gallery/types"))!;
        }
    }
}
