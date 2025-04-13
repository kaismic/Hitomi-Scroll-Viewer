using HitomiScrollViewerData;
using HitomiScrollViewerData.DbContexts;
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
                _lastLiveServerInfoUpdateTime = DateTime.UtcNow;
            }
        }
        private DateTime _lastLiveServerInfoUpdateTime = DateTime.MinValue;
        private readonly object _liveServerInfoUpdateLock = new();

        private readonly Dictionary<int, Downloader> _liveServerInfoUpdateWaiters = []; // note: _liveServerInfoUpdateWaiters is a subset of _liveDownloaders
        private readonly Dictionary<int, Downloader> _liveDownloaders = [];

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            ChannelReader<DownloadEventArgs> reader = eventBus.Subscribe();
            try {
                await foreach (DownloadEventArgs args in reader.ReadAllAsync(stoppingToken)) {
                     switch (args.Action) {
                        case DownloadAction.Start: {
                            logger.LogInformation("{GalleryId}: Start", args.GalleryId);
                            if (!_liveDownloaders.TryGetValue(args.GalleryId, out Downloader? downloader)) {
                                using (IServiceScope scope = serviceProvider.CreateScope()) {
                                    HitomiContext dbContext = scope.ServiceProvider.GetRequiredService<HitomiContext>();
                                    ICollection<int> downloads = dbContext.DownloadConfigurations.First().Downloads;
                                    if (!downloads.Contains(args.GalleryId)) {
                                        downloads.Add(args.GalleryId);
                                        dbContext.SaveChanges();
                                    }
                                }
                                downloader = new(serviceProvider.CreateScope()) {
                                    GalleryId = args.GalleryId,
                                    LiveServerInfo = LiveServerInfo,
                                    RemoveSelf = RemoveDownloader,
                                    RequestLiveServerInfoUpdate = UpdateLiveServerInfo
                                };
                            }
                            StartDownloader(downloader);
                            break;
                        }
                        case DownloadAction.Pause: {
                            
                            logger.LogInformation("{GalleryId}: Pause", args.GalleryId);
                            if (_liveDownloaders.TryGetValue(args.GalleryId, out Downloader? value)) {
                                // the pause request could have been sent at the exact timing when its LSI is being updated although it's very unlikely
                                // so just try to remove it from _liveServerInfoUpdateWaiters to prevent it from being started
                                _liveServerInfoUpdateWaiters.Remove(args.GalleryId);
                                value.Pause();
                            }
                            break;
                        }
                        case DownloadAction.Delete: {
                            logger.LogInformation("{GalleryId}: Delete", args.GalleryId);
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
            _liveDownloaders.Remove(d.GalleryId);
            _liveServerInfoUpdateWaiters.Remove(d.GalleryId);
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
            if (Monitor.TryEnter(_liveServerInfoUpdateLock)) {
                try {
                    LiveServerInfo = await GetLiveServerInfo();
                    foreach (Downloader d in _liveDownloaders.Values) {
                        d.LiveServerInfo = LiveServerInfo;
                    }
                    foreach (Downloader d in _liveServerInfoUpdateWaiters.Values) {
                        StartDownloader(d);
                    }
                    _liveServerInfoUpdateWaiters.Clear();
                } finally {
                    Monitor.Exit(_liveServerInfoUpdateLock);
                }
            }
        }

        private void StartDownloader(Downloader downloader) {
            _liveDownloaders.TryAdd(downloader.GalleryId, downloader);
            _ = downloader.Start();
        }

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

        public bool IsDownloading(int galleryId) {
            return _liveDownloaders.ContainsKey(galleryId);
        }
    }
}
