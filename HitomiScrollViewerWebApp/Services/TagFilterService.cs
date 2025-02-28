using HitomiScrollViewerData.DTOs;
using HitomiScrollViewerData.Entities;
using System.Net.Http.Json;

namespace HitomiScrollViewerWebApp.Services {
    public class TagFilterService(HttpClient httpClient) {
        private readonly HttpClient _httpClient = httpClient;

        public async Task<List<TagFilter>?> GetTagFiltersAsync() {
            return await _httpClient.GetFromJsonAsync<List<TagFilter>>("api/tagfilter/all");
        }

        public async Task<TagFilter?> GetTagFilterAsync(int id) {
            return await _httpClient.GetFromJsonAsync<TagFilter>($"api/tagfilter?id={id}");
        }
        
        public async Task<HttpResponseMessage> UpdateTagFilterAsync(int id, IEnumerable<TagDTO> tags) {
            return await _httpClient.PatchAsync($"api/tagfilter?id={id}", JsonContent.Create(tags));
        }
    }
}