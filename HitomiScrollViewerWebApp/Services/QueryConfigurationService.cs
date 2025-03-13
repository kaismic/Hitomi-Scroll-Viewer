using HitomiScrollViewerData.DTOs;
using System.Net.Http.Json;

namespace HitomiScrollViewerWebApp.Services {
    public class QueryConfigurationService(HttpClient httpClient) {

        public async Task<SearchQueryConfigurationDTO> GetSearchQueryConfigurationAsync() {
            return (await httpClient.GetFromJsonAsync<SearchQueryConfigurationDTO>("api/queryconfiguration/search"))!;
        }

        public async Task<BrowseQueryConfigurationDTO> GetBrowseQueryConfigurationAsync() {
            return (await httpClient.GetFromJsonAsync<BrowseQueryConfigurationDTO>("api/queryconfiguration/browse"))!;
        }

        // Search Query Configuration methods
        public async Task<bool> UpdateSearchIncludeTagFiltersAsync(int id, IEnumerable<int> tagFilterIds) {
            HttpResponseMessage response = await httpClient.PatchAsync($"api/queryconfiguration/search/include-tagfilters?id={id}", JsonContent.Create(tagFilterIds));
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UpdateSearchExcludeTagFiltersAsync(int id, IEnumerable<int> tagFilterIds) {
            HttpResponseMessage response = await httpClient.PatchAsync($"api/queryconfiguration/search/exclude-tagfilters?id={id}", JsonContent.Create(tagFilterIds));
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UpdateSearchSelectedTagFilterAsync(int id, int tagFilterId) {
            HttpResponseMessage response = await httpClient.PatchAsync($"api/queryconfiguration/search/selected-tagfilter?id={id}&tagFilterId={tagFilterId}", null);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UpdateSearchLanguageAsync(int id, int languageId) {
            HttpResponseMessage response = await httpClient.PatchAsync($"api/queryconfiguration/search/language?id={id}&languageId={languageId}", null);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UpdateSearchTypeAsync(int id, int typeId) {
            HttpResponseMessage response = await httpClient.PatchAsync($"api/queryconfiguration/search/type?id={id}&typeId={typeId}", null);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UpdateSearchKeywordTextAsync(int id, string searchKeywordText) {
            HttpResponseMessage response = await httpClient.PatchAsync($"api/queryconfiguration/search/SearchKeywordText?id={id}&searchKeywordText={searchKeywordText}", null);
            return response.IsSuccessStatusCode;
        }

        // Browse Query Configuration methods
        public async Task<bool> UpdateBrowseTagsAsync(int id, IEnumerable<int> tagIds) {
            HttpResponseMessage response = await httpClient.PatchAsync($"api/queryconfiguration/browse/tags?id={id}", JsonContent.Create(tagIds));
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UpdateBrowseLanguageAsync(int id, int languageId) {
            HttpResponseMessage response = await httpClient.PatchAsync($"api/queryconfiguration/browse/language?id={id}&languageId={languageId}", null);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UpdateBrowseTypeAsync(int id, int typeId) {
            HttpResponseMessage response = await httpClient.PatchAsync($"api/queryconfiguration/browse/type?id={id}&typeId={typeId}", null);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UpdateBrowseSearchKeywordTextAsync(int id, string searchKeywordText) {
            HttpResponseMessage response = await httpClient.PatchAsync($"api/queryconfiguration/browse/SearchKeywordText?id={id}&searchKeywordText={searchKeywordText}", null);
            return response.IsSuccessStatusCode;
        }
    }
}