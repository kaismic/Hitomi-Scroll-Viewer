using HitomiScrollViewerData.DTOs;
using HitomiScrollViewerData.Entities;
using System.Net.Http.Json;

namespace HitomiScrollViewerWebApp.Services {
    public class TagService(HttpClient httpClient) {
        private readonly HttpClient _httpClient = httpClient;

        public async Task<List<Tag>?> GetTagsAsync(TagCategory category, int count, string? start, CancellationToken ct) {
            string startStr = start == null || start.Length == 0 ? "" : $"&start={start}";
            try {
                return await _httpClient.GetFromJsonAsync<List<Tag>>($"api/tag/search?category={category}&count={count}{startStr}", ct);
            } catch (TaskCanceledException) {
                return null;
            }
        }

        public async Task<List<TagDTO>?> GetTags(int tagFilterId) {
            return await _httpClient.GetFromJsonAsync<List<TagDTO>>($"api/tag/tagfilter?tagFilterId={tagFilterId}");
        }

        public async Task<IEnumerable<TagDTO>?> GetTags(IEnumerable<int> ids) {
            HttpResponseMessage response = await _httpClient.PostAsync("api/tag/tagfilter", JsonContent.Create(ids));
            return await response.Content.ReadFromJsonAsync<IEnumerable<TagDTO>>();
        }
    }
}
