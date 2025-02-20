using HitomiScrollViewerData.Entities;
using System.Net.Http.Json;

namespace HitomiScrollViewerWebApp.Services {
    public class TagService(HttpClient httpClient) {
        private readonly HttpClient _httpClient = httpClient;

        public async Task<List<Tag>?> GetTagsAsync(TagCategory category, int count, string? start) {
            Console.WriteLine("start = " + start);
            string startStr = start == null || start.Length == 0 ? "" : $"&start={start}";
            return await _httpClient.GetFromJsonAsync<List<Tag>>($"api/tag?category={category}&count={count}{startStr}");
        }
    }
}
