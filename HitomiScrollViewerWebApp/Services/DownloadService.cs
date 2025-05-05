using System.Net.Http.Json;

namespace HitomiScrollViewerWebApp.Services {
    public class DownloadService(HttpClient httpClient) {
        public async Task<bool> Create(int galleryId) {
            HttpResponseMessage response = await httpClient.PostAsync($"create", JsonContent.Create(galleryId));
            return response.IsSuccessStatusCode;
        }
        
        public async Task<bool> Start(int galleryId) {
            HttpResponseMessage response = await httpClient.PostAsync($"start", JsonContent.Create(galleryId));
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> Pause(int galleryId) {
            HttpResponseMessage response = await httpClient.PostAsync($"pause", JsonContent.Create(galleryId));
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> Delete(int galleryId) {
            HttpResponseMessage response = await httpClient.PostAsync($"delete", JsonContent.Create(galleryId));
            return response.IsSuccessStatusCode;
        }
    }
}
