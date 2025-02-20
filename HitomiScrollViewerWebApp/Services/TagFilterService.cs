using HitomiScrollViewerData.Entities;
using System.Net.Http.Json;

namespace HitomiScrollViewerWebApp.Services {
    public class TagFilterService(HttpClient httpClient) {
        private readonly HttpClient _httpClient = httpClient;

        public async Task<List<TagFilter>?> GetTagFiltersAsync() {
            return await _httpClient.GetFromJsonAsync<List<TagFilter>>("api/tagfilter");
        }

        public async Task<TagFilter?> GetTagFilterAsync(int id) {
            return await _httpClient.GetFromJsonAsync<TagFilter>($"api/tagfilter/{id}");
        }
    }
}
