using HitomiScrollViewerData.DTOs;
using HitomiScrollViewerData.Entities;
using System.Net.Http.Json;

namespace HitomiScrollViewerWebApp.Services {
    public class TagFilterService(HttpClient httpClient) {
        private readonly HttpClient _httpClient = httpClient;

        public async Task<IEnumerable<TagFilterDTO>?> GetTagFiltersAsync() {
            return await _httpClient.GetFromJsonAsync<IEnumerable<TagFilterDTO>>("api/tagfilter/all");
        }

        public async Task<TagFilterDTO?> GetTagFilterAsync(int id) {
            return await _httpClient.GetFromJsonAsync<TagFilterDTO>($"api/tagfilter?id={id}");
        }
        
        public async Task<HttpResponseMessage> UpdateTagFilterAsync(int id, IEnumerable<TagDTO> tags) {
            return await _httpClient.PatchAsync($"api/tagfilter?id={id}", JsonContent.Create(tags));
        }

        public async Task<TagFilterDTO?> CreateTagFilterAsync(string name, IEnumerable<TagDTO> tags) {
            HttpResponseMessage response = await _httpClient.PostAsync($"api/tagfilter?name={name}", JsonContent.Create(tags));
            return await response.Content.ReadFromJsonAsync<TagFilterDTO>();
        }
    }
}