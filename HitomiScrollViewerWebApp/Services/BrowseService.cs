using HitomiScrollViewerData.DTOs;
using System.Net.Http.Json;

namespace HitomiScrollViewerWebApp.Services {
    public class BrowseService(HttpClient httpClient) {
        public async Task<BrowseConfigurationDTO> GetConfigurationAsync() {
            return (await httpClient.GetFromJsonAsync<BrowseConfigurationDTO>("api/browse"))!;
        }

        public async Task<bool> AddTagsAsync(int configId, IEnumerable<int> tagIds) {
            var response = await httpClient.PatchAsync($"api/browse/add-tags?configId={configId}", JsonContent.Create(tagIds));
            return response.IsSuccessStatusCode;
        }
        
        public async Task<bool> RemoveTagsAsync(int configId, IEnumerable<int> tagIds) {
            var response = await httpClient.PatchAsync($"api/browse/remove-tags?configId={configId}", JsonContent.Create(tagIds));
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UpdateSearchKeywordTextAsync(int configId, string searchKeywordText) {
            var response = await httpClient.PatchAsync($"api/browse/search-keyword-text?configId={configId}", JsonContent.Create(searchKeywordText));
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UpdateLanguageAsync(int configId, int languageId) {
            var response = await httpClient.PatchAsync($"api/browse/language?configId={configId}&languageId={languageId}", null);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UpdateTypeAsync(int configId, int typeId) {
            var response = await httpClient.PatchAsync($"api/browse/type?configId={configId}&typeId={typeId}", null);
            return response.IsSuccessStatusCode;
        }
    }
}
