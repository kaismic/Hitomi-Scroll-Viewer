using HitomiScrollViewerData.DTOs;
using System.Net.Http.Json;

namespace HitomiScrollViewerWebApp.Services {
    public class DownloadConfigurationService(HttpClient httpClient) {
        public bool IsLoaded { get; private set; } = false;
        public DownloadConfigurationDTO Config { get; private set; } = new();

        public async Task Load() {
            Config = (await httpClient.GetFromJsonAsync<DownloadConfigurationDTO>(""))!;
            IsLoaded = true;
        }

        public async Task<bool> UpdateParallelDownload(bool enable) {
            var response = await httpClient.PatchAsync($"update-parallel-download?configId={Config.Id}", JsonContent.Create(enable));
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UpdateThreadNum(int threadNum) {
            var response = await httpClient.PatchAsync($"update-thread-num?configId={Config.Id}", JsonContent.Create(threadNum));
            return response.IsSuccessStatusCode;
        }
    }
}
