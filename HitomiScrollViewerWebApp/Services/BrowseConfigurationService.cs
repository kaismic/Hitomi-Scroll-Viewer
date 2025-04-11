using HitomiScrollViewerData.DTOs;
using System.Net.Http.Json;

namespace HitomiScrollViewerWebApp.Services {
    public class BrowseConfigurationService(HttpClient httpClient) {
        public bool IsLoaded { get; private set; } = false;
        public BrowseConfigurationDTO Config { get; private set; } = new() {
            SelectedLanguage = new() { EnglishName = "", Id = 0, IsAll = true, LocalName = "" },
            SelectedType = new() { Id = 0, IsAll = true, Value = "" },
            SearchKeywordText = "",
            ItemsPerPage = 8,
            Tags = []
        };

        public async Task Load() {
            Config = (await httpClient.GetFromJsonAsync<BrowseConfigurationDTO>(""))!;
            IsLoaded = true;
        }

        public async Task<bool> AddTagsAsync(IEnumerable<int> tagIds) {
            var response = await httpClient.PatchAsync($"add-tags?configId={Config.Id}", JsonContent.Create(tagIds));
            return response.IsSuccessStatusCode;
        }
        
        public async Task<bool> RemoveTagsAsync(IEnumerable<int> tagIds) {
            var response = await httpClient.PatchAsync($"remove-tags?configId={Config.Id}", JsonContent.Create(tagIds));
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UpdateSearchKeywordTextAsync(string searchKeywordText) {
            var response = await httpClient.PatchAsync($"search-keyword-text?configId={Config.Id}", JsonContent.Create(searchKeywordText));
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UpdateLanguageAsync(int languageId) {
            var response = await httpClient.PatchAsync($"language?configId={Config.Id}", JsonContent.Create(languageId));
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UpdateTypeAsync(int typeId) {
            var response = await httpClient.PatchAsync($"type?configId={Config.Id}", JsonContent.Create(typeId));
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UpdateItemsPerPageAsync(int value) {
            var response = await httpClient.PatchAsync($"items-per-page?configId={Config.Id}", JsonContent.Create(value));
            return response.IsSuccessStatusCode;
        }
    }
}
