using HitomiScrollViewerData.DTOs;
using System.Net.Http.Json;

namespace HitomiScrollViewerWebApp.Services {
    public class TagFilterService(HttpClient httpClient) {
        public async Task<TagFilterDTO> GetAsync(int configId, int tagFilterId) {
            return (await httpClient.GetFromJsonAsync<TagFilterDTO>($"api/search/tag-filter?configId={configId}&tagFilterId={tagFilterId}"))!;
        }

        public async Task<int> CreateAsync(TagFilterBuildDTO dto) {
            var response = await httpClient.PostAsJsonAsync($"api/search/tag-filter", dto);
            return await response.Content.ReadFromJsonAsync<int>();
        }

        public async Task<IEnumerable<TagFilterDTO>> GetAllAsync(int configId) {
            return (await httpClient.GetFromJsonAsync<IEnumerable<TagFilterDTO>>($"api/search/tag-filter/all?configId={configId}"))!;
        }

        public async Task<bool> DeleteAsync(int configId, IEnumerable<int> tagFilterIds) {
            var response = await httpClient.PostAsJsonAsync($"api/search/tag-filter/delete?configId={configId}", tagFilterIds);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UpdateNameAsync(int configId, int tagFilterId, string name) {
            var response = await httpClient.PatchAsync($"api/search/tag-filter/name?configId={configId}&tagFilterId={tagFilterId}",
                JsonContent.Create(name));
            return response.IsSuccessStatusCode;
        }

        public async Task<IEnumerable<TagDTO>> GetTagsAsync(int configId, int tagFilterId) {
            return (await httpClient.GetFromJsonAsync<IEnumerable<TagDTO>>($"api/search/tag-filter/tags?configId={configId}&tagFilterId={tagFilterId}"))!;
        }

        public async Task<bool> UpdateTagsAsync(int configId, int tagFilterId, IEnumerable<int> tagIds) {
            var response = await httpClient.PatchAsync($"api/search/tag-filter/tags?configId={configId}&tagFilterId={tagFilterId}", JsonContent.Create(tagIds));
            return response.IsSuccessStatusCode;
        }

        public async Task<IEnumerable<TagDTO>> GetTagsUnionAsync(int configId, IEnumerable<int> tagFilterIds) {
            var response = await httpClient.PostAsJsonAsync($"api/search/tag-filter/tags-union?configId={configId}", tagFilterIds);
            return (await response.Content.ReadFromJsonAsync<IEnumerable<TagDTO>>())!;
        }
    }
}