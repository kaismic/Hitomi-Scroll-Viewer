using HitomiScrollViewerData.DTOs;
using System.Net.Http.Json;

namespace HitomiScrollViewerWebApp.Services {
    public class GalleryService(HttpClient httpClient) {
        private readonly HttpClient _httpClient = httpClient;

        public async Task<List<GalleryLanguageDTO>> GetGalleryLanguages() {
            return (await _httpClient.GetFromJsonAsync<List<GalleryLanguageDTO>>("api/gallery/languages"))!;
        }

        public async Task<List<GalleryTypeDTO>> GetGalleryTypes() {
            return (await _httpClient.GetFromJsonAsync<List<GalleryTypeDTO>>("api/gallery/types"))!;
        }
    }
}
