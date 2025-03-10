using HitomiScrollViewerData.DTOs;
using System.Net.Http.Json;

namespace HitomiScrollViewerWebApp.Services {
    public class SearchFilterService(HttpClient httpClient) {
        private readonly HttpClient _httpClient = httpClient;

        public async Task<IEnumerable<SearchFilterDTO>> GetSearchFilters() {
            return (await _httpClient.GetFromJsonAsync<IEnumerable<SearchFilterDTO>>("api/searchfilter/all"))!;
        }

        // TODO implement this
        // TODO move javascript code to razor.js
    }
}
