using HitomiScrollViewerData.DTOs;
using System.Net.Http.Json;

namespace HitomiScrollViewerWebApp.Services {
    public class SearchService(HttpClient httpClient) {
        public async Task<SearchConfigurationDTO> GetConfigurationAsync() {
            return (await httpClient.GetFromJsonAsync<SearchConfigurationDTO>("api/search"))!;
        }

        public async Task<bool> UpdateAutoSaveAsync(int configId, bool enable) {
            var response = await httpClient.PatchAsync($"api/search/enable-auto-save?configId={configId}&enable={enable}", null);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UpdateIncludeTagFiltersAsync(int configId, IEnumerable<int> tagFilterIds) {
            var response = await httpClient.PatchAsync($"api/search/include-tag-filters?configId={configId}", JsonContent.Create(tagFilterIds));
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UpdateExcludeTagFiltersAsync(int configId, IEnumerable<int> tagFilterIds) {
            var response = await httpClient.PatchAsync($"api/search/exclude-tag-filters?configId={configId}", JsonContent.Create(tagFilterIds));
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UpdateSelectedTagFilterAsync(int configId, int tagFilterId) {
            var response = await httpClient.PatchAsync($"api/search/selected-tag-filter?configId={configId}&tagFilterId={tagFilterId}", null);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UpdateSearchKeywordTextAsync(int configId, string searchKeywordText) {
            var response = await httpClient.PatchAsync($"api/search/search-keyword-text?configId={configId}", JsonContent.Create(searchKeywordText));
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UpdateLanguageAsync(int configId, int languageId) {
            var response = await httpClient.PatchAsync($"api/search/language?configId={configId}&languageId={languageId}", null);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UpdateTypeAsync(int configId, int typeId) {
            var response = await httpClient.PatchAsync($"api/search/type?configId={configId}&typeId={typeId}", null);
            return response.IsSuccessStatusCode;
        }
    }
}
