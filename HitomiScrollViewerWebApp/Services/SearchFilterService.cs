using HitomiScrollViewerData.DTOs;
using System.Net.Http.Json;

namespace HitomiScrollViewerWebApp.Services {
    public class SearchFilterService(HttpClient httpClient) {
        public async Task<int> CreateAsync(int configId, SearchFilterDTO searchFilter) {
            var response = await httpClient.PostAsJsonAsync($"api/search/search-filter?configId={configId}", searchFilter);
            return await response.Content.ReadFromJsonAsync<int>();
        }

        public async Task<bool> DeleteAsync(int configId, int searchFilterId) {
            var response = await httpClient.DeleteAsync($"api/search/search-filter?configId={configId}&searchFilterId={searchFilterId}");
            return response.IsSuccessStatusCode;
        }
        
        public async Task<bool> ClearAsync(int configId) {
            var response = await httpClient.DeleteAsync($"api/search/search-filter/clear?configId={configId}");
            return response.IsSuccessStatusCode;
        }
    }
}
