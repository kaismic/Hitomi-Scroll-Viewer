using HitomiScrollViewerData;
using HitomiScrollViewerData.DbContexts;
using HitomiScrollViewerData.Entities;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using System.Threading.Channels;

namespace HitomiScrollViewerAPI.Download {
    public class DownloadManagerService
        (
            IServiceProvider serviceProvider,
            ILogger<DownloadManagerService> logger,
            IEventBus<DownloadEventArgs> eventBus,
            IConfiguration appConfiguration,
            HttpClient httpClient
        ) : BackgroundService {
        private const int SERVER_TIME_EXCLUDE_LENGTH = 16; // length of the string "0123456789/'\r\n};"
        private readonly string _hitomiGgjsAddress = $"https://ltn.{appConfiguration["HitomiServerDomain"]}/gg.js";
        private bool _lsiInitialized = false;
        public LiveServerInfo LiveServerInfo { get; private set; } = new();

        private readonly ConcurrentDictionary<int, Downloader> _liveDownloaders = [];

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            ChannelReader<DownloadEventArgs> reader = eventBus.Subscribe();
            try {
                await foreach (DownloadEventArgs args in reader.ReadAllAsync(stoppingToken)) {
                     switch (args.Action) {
                        case DownloadAction.Create: {
                            GetOrCreateDownloader(args.GalleryId);
                            break;
                        }
                        case DownloadAction.Start: {
                            if (!_lsiInitialized) {
                                logger.LogInformation("Fetching Live Server Info...");
                                try {
                                    await UpdateLiveServerInfo();
                                    _lsiInitialized = true;
                                } catch (HttpRequestException e) {
                                    logger.LogError(e, "Failed to fetch Live Server Info.");
                                    break;
                                }
                            }
                            Downloader downloader = GetOrCreateDownloader(args.GalleryId);
                            _ = downloader.Start();
                            break;
                        }
                        case DownloadAction.Pause: {
                            if (_liveDownloaders.TryGetValue(args.GalleryId, out Downloader? value)) {
                                await value.Pause();
                            }
                            break;
                        }
                        case DownloadAction.Delete: {
                            if (_liveDownloaders.TryGetValue(args.GalleryId, out Downloader? value)) {
                                await value.Delete();
                            }
                            break;
                        }
                    }
                }
            } catch (OperationCanceledException) {
            } catch (Exception e) {
                logger.LogError(e, "");
            }
        }

        private Downloader GetOrCreateDownloader(int galleryId) {
            return _liveDownloaders.GetOrAdd(galleryId, (galleryId) => {
                logger.LogInformation("{GalleryId}: Creating Downloader.", galleryId);
                IServiceScope scope = serviceProvider.CreateScope();
                using (HitomiContext dbContext = scope.ServiceProvider.GetRequiredService<HitomiContext>()) {
                    ICollection<int> downloads = dbContext.DownloadConfigurations.First().Downloads;
                    if (!downloads.Contains(galleryId)) {
                        downloads.Add(galleryId);
                        dbContext.SaveChanges();
                    }
                }
                return new(scope) {
                    GalleryId = galleryId,
                    DownloadManagerService = this,
                    RemoveSelf = RemoveDownloader
                };
            });
        }

        private void RemoveDownloader(Downloader downloader) {
            HitomiContext dbContext = new();
            DownloadConfiguration config = dbContext.DownloadConfigurations.First();
            ICollection<int> downloads = config.Downloads;
            bool useParallelDownload = config.UseParallelDownload;
            if (downloads.Contains(downloader.GalleryId)) {
                downloads.Remove(downloader.GalleryId);
                dbContext.SaveChanges();
            }
            dbContext.Dispose();
            _liveDownloaders.TryRemove(downloader.GalleryId, out _);
            downloader.Dispose();
            if (!useParallelDownload) {
                Downloader? firstPaused = null;
                foreach (Downloader d in _liveDownloaders.Values) {
                    if (d.Status == DownloadStatus.Downloading) {
                        return;
                    } else if (firstPaused == null && d.Status == DownloadStatus.Paused) {
                        firstPaused = d;
                    }
                }
                // no currently downloading downloads so start the first paused download
                if (firstPaused != null) {
                    _ = firstPaused.Start();
                }
            }
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <exception cref="HttpRequestException"></exception>
        /// <returns></returns>
        public async Task UpdateLiveServerInfo() {
            HttpResponseMessage response = await httpClient.GetAsync(_hitomiGgjsAddress);
            response.EnsureSuccessStatusCode();

            string content = await response.Content.ReadAsStringAsync();

            string serverTime = content.Substring(content.Length - SERVER_TIME_EXCLUDE_LENGTH, 10);
            string selectionSetPat = @"case (\d+)";
            MatchCollection matches = Regex.Matches(content, selectionSetPat);
            HashSet<string> subdomainSelectionSet = [.. matches.Select(match => match.Groups[1].Value)];

            string orderPat = @"var [a-z] = (\d);";
            Match match = Regex.Match(content, orderPat);
            LiveServerInfo = new() {
                ServerTime = int.Parse(serverTime),
                SubdomainSelectionSet = subdomainSelectionSet,
                IsContains = match.Groups[1].Value == "0"
            };
        }
    }
}
