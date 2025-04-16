using HitomiScrollViewerData.DTOs;
using System.Net.Http.Json;

namespace HitomiScrollViewerWebApp.Services {
    public class ViewConfigurationService(HttpClient httpClient) {
        public async Task<ViewConfigurationDTO> GetConfiguration() {
            return (await httpClient.GetFromJsonAsync<ViewConfigurationDTO>(""))!;
        }
    }
}
