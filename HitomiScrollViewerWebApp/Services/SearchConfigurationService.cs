using HitomiScrollViewerData.DTOs;
using System.Net.Http.Json;

namespace HitomiScrollViewerWebApp.Services {
    public class SearchConfigurationService(HttpClient httpClient) {
        public bool IsLoaded { get; private set; } = false;
        public SearchConfigurationDTO Config { get; private set; } = new() {
            SearchFilters = [],
            SearchKeywordText = "",
            SelectedExcludeTagFilterIds = [],
            SelectedIncludeTagFilterIds = [],
            SelectedTagFilterId = 0,
            SelectedLanguage = new() { EnglishName = "", Id = 0, IsAll = true, LocalName = "" },
            SelectedType = new() { Id = 0, IsAll = true, Value = "" },
            TagFilters = []
        };

        public async Task Load() {
            Config = (await httpClient.GetFromJsonAsync<SearchConfigurationDTO>(""))!;
            IsLoaded = true;
        }

        public async Task<bool> UpdateAutoSaveAsync(bool enable) {
            var response = await httpClient.PatchAsync($"enable-auto-save?configId={Config.Id}&enable={enable}", null);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UpdateTagFilterCollectionAsync(bool isInclude, IEnumerable<int> tagFilterIds) {
            var response = await httpClient.PatchAsync($"tag-filter-collection?configId={Config.Id}&isInclude={isInclude}", JsonContent.Create(tagFilterIds));
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UpdateSelectedTagFilterAsync(int tagFilterId) {
            var response = await httpClient.PatchAsync($"selected-tag-filter?configId={Config.Id}&tagFilterId={tagFilterId}", null);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UpdateSearchKeywordTextAsync(string searchKeywordText) {
            var response = await httpClient.PatchAsync($"search-keyword-text?configId={Config.Id}", JsonContent.Create(searchKeywordText));
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UpdateLanguageAsync(int languageId) {
            var response = await httpClient.PatchAsync($"language?configId={Config.Id}&languageId={languageId}", null);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UpdateTypeAsync(int typeId) {
            var response = await httpClient.PatchAsync($"type?configId={Config.Id}&typeId={typeId}", null);
            return response.IsSuccessStatusCode;
        }
    }
}
