using HitomiScrollViewerData.DTOs;
using System.Net.Http.Json;

namespace HitomiScrollViewerWebApp.Services {
    public class SearchFilterService(HttpClient httpClient) {
        public async Task<IEnumerable<SearchFilterDTO>> GetSearchFiltersAsync() {
            return (await httpClient.GetFromJsonAsync<IEnumerable<SearchFilterDTO>>("api/searchfilter/all"))!;
        }
        public async Task<bool> CreateSearchFilterAsync(SearchFilterDTO searchFilter) {
            HttpResponseMessage response = await httpClient.PostAsync("api/searchfilter/create", JsonContent.Create(searchFilter));
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> DeleteSearchFilterAsync(int id) {
            HttpResponseMessage response = await httpClient.DeleteAsync($"api/searchfilter/delete?id={id}");
            return response.IsSuccessStatusCode;
        }
    }
}
