using HitomiScrollViewerData.DTOs;
using HitomiScrollViewerWebApp.ViewModels;
using System.Collections.Concurrent;
using System.Net.Http.Json;

namespace HitomiScrollViewerWebApp.Services {
    public class DownloadService
        (
            HttpClient httpClient,
            GalleryService galleryService,
            IConfiguration appConfiguration,
            PageConfigurationService pageConfigurationService
        ) {
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

        private readonly string _downloadHubUrl = appConfiguration["ApiUrl"] + appConfiguration["DownloadHubPath"];
        public ConcurrentDictionary<int, DownloadViewModel> Downloads { get; } = [];

        public void CreateDownloads(IEnumerable<int> galleryIds) {
            foreach (int id in galleryIds) {
                if (Downloads.ContainsKey(id)) {
                    continue;
                }
                DownloadViewModel vm = new() {
                    GalleryService = galleryService,
                    PageConfigurationService = pageConfigurationService,
                    DownloadHubUrl = _downloadHubUrl,
                    GalleryId = id,
                };
                _ = vm.StartDownload();
                Downloads.TryAdd(id, vm);
            }
        }

        public void RemoveDownload(int id) {
            if (Downloads.Remove(id, out DownloadViewModel? vm)) {
                _ = vm.SendDisconnect();
            }
        }
    }
}
