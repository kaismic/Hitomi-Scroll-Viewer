using HitomiScrollViewerData.DTOs;
using System.Net.Http.Json;

namespace HitomiScrollViewerWebApp.Services {
    public class AppConfigurationService(HttpClient httpClient) {
        private bool _isLoaded = false;
        public AppConfigurationDTO Config { get; private set; } = new();

        public async Task Load() {
            if (_isLoaded) {
                return;
            }
            Config = (await httpClient.GetFromJsonAsync<AppConfigurationDTO>(""))!;
            _isLoaded = true;
        }

        public async Task<bool> UpdateIsFirstLaunch(bool value) {
            var response = await httpClient.PatchAsync($"is-first-launch?configId={Config.Id}", JsonContent.Create(value));
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UpdateAppLanguage(string value) {
            var response = await httpClient.PatchAsync($"app-language?configId={Config.Id}", JsonContent.Create(value));
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UpdateLastUpdateCheckTime(DateTimeOffset value) {
            var response = await httpClient.PatchAsync($"last-update-check-time?configId={Config.Id}", JsonContent.Create(value));
            return response.IsSuccessStatusCode;
        }
    }
}
