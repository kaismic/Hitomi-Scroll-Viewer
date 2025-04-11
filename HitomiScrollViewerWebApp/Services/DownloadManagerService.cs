using HitomiScrollViewerData;
using HitomiScrollViewerWebApp.ViewModels;

namespace HitomiScrollViewerWebApp.Services {
    public class DownloadManagerService
        (
            GalleryService galleryService,
            IConfiguration appConfiguration,
            DownloadConfigurationService downloadConfigurationService
        ) {
        private readonly string _downloadHubUrl = appConfiguration["ApiUrl"] + appConfiguration["DownloadHubPath"];
        public bool IsLoaded { get; private set; } = false;
        public Dictionary<int, DownloadViewModel> Downloads { get; private set; } = [];

        public async Task Load() {
            Downloads =
                (await downloadConfigurationService.GetDownloads())
                    .Select(galleryId => KeyValuePair.Create(
                        galleryId,
                        new DownloadViewModel() {
                            GalleryService = galleryService,
                            DownloadHubUrl = _downloadHubUrl,
                            GalleryId = galleryId,
                            OnDownloadCompleted = OnDownloadCompleted,
                            Status = DownloadStatus.Paused
                        }
                    )
                ).ToDictionary();
            IsLoaded = true;
        }

        public void CreateDownloads(IEnumerable<int> galleryIds) {
            foreach (int id in galleryIds) {
                if (Downloads.ContainsKey(id)) {
                    continue;
                }
                DownloadViewModel vm = new() {
                    GalleryService = galleryService,
                    DownloadHubUrl = _downloadHubUrl,
                    GalleryId = id,
                    OnDownloadCompleted = OnDownloadCompleted
                };
                Downloads.TryAdd(id, vm);
                if (downloadConfigurationService.Config.UseParallelDownload) {
                    _ = vm.StartDownload();
                }
            }
            if (!downloadConfigurationService.Config.UseParallelDownload) {
                foreach (DownloadViewModel d in Downloads.Values) {
                    if (d.Status == DownloadStatus.Downloading) {
                        return;
                    }
                }
                // no currently downloading downloads so find a pending download and start it
                foreach (DownloadViewModel d in Downloads.Values) {
                    if (d.Status == DownloadStatus.Pending) {
                        _ = d.StartDownload();
                        return;
                    }
                }
            }
            _ = downloadConfigurationService.AddDownloads(galleryIds);
        }

        public void StartDownload(int id) {
            _ = Downloads[id].StartDownload();
        }

        public void PauseDownload(int id) {
            _ = Downloads[id].Pause();
        }


        public void RemoveDownload(int id) {
            if (Downloads.Remove(id, out DownloadViewModel? vm)) {
                _ = vm.Remove();
            }
        }

        public void OnDownloadCompleted(int id) {
            if (Downloads.Remove(id, out DownloadViewModel? vm)) {
                _ = vm.Remove();
            }
            if (!downloadConfigurationService.Config.UseParallelDownload) {
                foreach (DownloadViewModel d in Downloads.Values) {
                    if (d.Status == DownloadStatus.Downloading) {
                        return;
                    }
                }
                // no currently downloading downloads so find a pending download and start it
                foreach (DownloadViewModel d in Downloads.Values) {
                    if (d.Status == DownloadStatus.Pending) {
                        _ = d.StartDownload();
                        return;
                    }
                }
            }
        }
    }
}
