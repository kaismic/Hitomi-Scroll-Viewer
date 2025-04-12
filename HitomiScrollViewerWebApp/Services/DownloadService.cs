namespace HitomiScrollViewerWebApp.Services {
    public class DownloadService(HttpClient httpClient) {
        public async Task StartDownload(int galleryId) {
            await httpClient.PostAsync($"start?galleryId={galleryId}", null);
        }

        public async Task PauseDownload(int galleryId) {
            await httpClient.PostAsync($"pause?galleryId={galleryId}", null);
        }

        public async Task DeleteDownload(int galleryId) {
            await httpClient.DeleteAsync($"delete?galleryId={galleryId}");
        }
    }
}
