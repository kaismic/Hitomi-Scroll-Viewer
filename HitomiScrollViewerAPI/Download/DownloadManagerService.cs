using HitomiScrollViewerData;
using HitomiScrollViewerData.DbContexts;
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
        private LiveServerInfo? _liveServerInfo;
        private LiveServerInfo? LiveServerInfo {
            get => _liveServerInfo;
            set {
                _liveServerInfo = value;
                _lastLiveServerInfoUpdateTime = DateTimeOffset.UtcNow;
            }
        }
        private DateTimeOffset _lastLiveServerInfoUpdateTime = DateTimeOffset.MinValue;
        private readonly object _liveServerInfoUpdateLock = new();

        private readonly ConcurrentDictionary<int, Downloader> _liveServerInfoUpdateWaiters = []; // note: _liveServerInfoUpdateWaiters is a subset of _liveDownloaders
        private readonly ConcurrentDictionary<int, Downloader> _liveDownloaders = [];

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            ChannelReader<DownloadEventArgs> reader = eventBus.Subscribe();
            try {
                await foreach (DownloadEventArgs args in reader.ReadAllAsync(stoppingToken)) {
                     switch (args.Action) {
                        case DownloadAction.Start: {
                            logger.LogInformation("{GalleryId}: Start.", args.GalleryId);
                            if (LiveServerInfo == null) {
                                logger.LogInformation("Fetching Live Server Info...");
                                try {
                                    LiveServerInfo = await GetLiveServerInfo();
                                } catch (HttpRequestException e) {
                                    logger.LogError(e, "Failed to fetch Live Server Info.");
                                    break;
                                }
                            }
                            Downloader downloader = _liveDownloaders.GetOrAdd(args.GalleryId, (galleryId) => {
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
                                    LiveServerInfo = LiveServerInfo,
                                    RemoveSelf = RemoveDownloader,
                                    RequestLiveServerInfoUpdate = UpdateLiveServerInfo
                                };
                            });
                            // Whether the downloader was retrieved or newly created, start it.
                            _ = downloader.Start();
                            break;
                        }
                        case DownloadAction.Pause: {
                            logger.LogInformation("{GalleryId}: Pause.", args.GalleryId);
                            if (_liveDownloaders.TryGetValue(args.GalleryId, out Downloader? value)) {
                                // the pause request could have been sent at the exact timing when its LSI is being updated although it's very unlikely
                                // so try to remove it from _liveServerInfoUpdateWaiters to prevent it from being started
                                _liveServerInfoUpdateWaiters.TryRemove(args.GalleryId, out _);
                                value.Pause();
                            }
                            break;
                        }
                        case DownloadAction.Delete: {
                            logger.LogInformation("{GalleryId}: Delete.", args.GalleryId);
                            if (_liveDownloaders.TryGetValue(args.GalleryId, out Downloader? value)) {
                                RemoveDownloader(value);
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

        private void RemoveDownloader(Downloader d) {
            using (IServiceScope scope = serviceProvider.CreateScope()) {
                HitomiContext dbContext = scope.ServiceProvider.GetRequiredService<HitomiContext>();
                ICollection<int> downloads = dbContext.DownloadConfigurations.First().Downloads;
                if (downloads.Contains(d.GalleryId)) {
                    downloads.Remove(d.GalleryId);
                    dbContext.SaveChanges();
                }
            }
            _liveDownloaders.TryRemove(d.GalleryId, out _);
            _liveServerInfoUpdateWaiters.TryRemove(d.GalleryId, out _);
            d.Dispose();
        }

        private async Task UpdateLiveServerInfo(Downloader downloader) {
            if (LiveServerInfo != null) {
                if (downloader.LastLiveServerInfoUpdateTime < _lastLiveServerInfoUpdateTime) {
                    downloader.LiveServerInfo = LiveServerInfo;
                    StartDownloader(downloader);
                    return;
                }
            }
            _liveServerInfoUpdateWaiters.TryAdd(downloader.GalleryId, downloader);

            if (Monitor.TryEnter(_liveServerInfoUpdateLock)) { // Consider SemaphoreSlim here too
                try {
                    LiveServerInfo = await GetLiveServerInfo();

                    // Update *all* downloaders (safe to iterate ConcurrentDictionary)
                    foreach (var kvp in _liveDownloaders) {
                        kvp.Value.LiveServerInfo = LiveServerInfo;
                    }

                    // Capture waiters *before* clearing, iterate the capture
                    var waitersToStart = _liveServerInfoUpdateWaiters.Values.ToList();
                    _liveServerInfoUpdateWaiters.Clear(); // Clear is safe

                    foreach (Downloader d in waitersToStart) {
                        // Check if the downloader wasn't paused/removed *after* being added to waiters
                        // but *before* this loop runs.
                        if (_liveDownloaders.ContainsKey(d.GalleryId) && d.Status == DownloadStatus.WaitingLSIUpdate) {
                            _ = d.Start(); // Use fire-and-forget
                        }
                    }
                } finally {
                    Monitor.Exit(_liveServerInfoUpdateLock);
                }
            }
        }

        private void StartDownloader(Downloader downloader) {
            _liveDownloaders.TryAdd(downloader.GalleryId, downloader);
            _ = downloader.Start();
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <exception cref="HttpRequestException"></exception>
        /// <returns></returns>
        private async Task<LiveServerInfo> GetLiveServerInfo() {
            HttpResponseMessage response = await httpClient.GetAsync(_hitomiGgjsAddress);
            response.EnsureSuccessStatusCode();

            string content = await response.Content.ReadAsStringAsync();

            string serverTime = content.Substring(content.Length - SERVER_TIME_EXCLUDE_LENGTH, 10);
            string selectionSetPat = @"case (\d+)";
            MatchCollection matches = Regex.Matches(content, selectionSetPat);
            HashSet<string> subdomainSelectionSet = [.. matches.Select(match => match.Groups[1].Value)];

            string orderPat = @"var [a-z] = (\d);";
            Match match = Regex.Match(content, orderPat);
            return new() {
                ServerTime = serverTime,
                SubdomainSelectionSet = subdomainSelectionSet,
                IsContains = match.Groups[1].Value == "0"
            };
        }
    }
}
