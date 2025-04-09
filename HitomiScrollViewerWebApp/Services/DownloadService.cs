using HitomiScrollViewerData.DTOs;
using System.Net.Http.Json;

namespace HitomiScrollViewerWebApp.Services {
    public class DownloadService(HttpClient httpClient) {
        public async Task<DownloadConfigurationDTO> GetConfigurationAsync() {
            return (await httpClient.GetFromJsonAsync<DownloadConfigurationDTO>("api/download"))!;
        }

        public async Task<bool> UpdateParallelDownload(int configId, bool enable) {
            var response = await httpClient.PatchAsync($"api/download/update-parallel-download?configId={configId}&enable={enable}", null);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UpdateThreadNum(int configId, int threadNum) {
            var response = await httpClient.PatchAsync($"api/download/update-thread-num?configId={configId}&threadNum={threadNum}", null);
            return response.IsSuccessStatusCode;
        }
    }
}
