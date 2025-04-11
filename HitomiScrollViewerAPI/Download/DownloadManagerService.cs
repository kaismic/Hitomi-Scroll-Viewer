using HitomiScrollViewerAPI.Hubs;
using HitomiScrollViewerData;
using HitomiScrollViewerData.DbContexts;
using Microsoft.AspNetCore.SignalR;
using System.Text.RegularExpressions;
using System.Threading.Channels;

namespace HitomiScrollViewerAPI.Download {
    public class DownloadManagerService
        (
            IServiceProvider serviceProvider,
            ILogger<DownloadManagerService> logger,
            IEventBus<DownloadEventArgs> eventBus,
            IHubContext<DownloadHub, IDownloadClient> hubContext,
            IConfiguration appConfiguration,
            HttpClient httpClient
        ) : BackgroundService {
        private const int SERVER_TIME_EXCLUDE_LENGTH = 16; // length of the string "0123456789/'\r\n};"
        private readonly string _hitomiGgjsAddress = $"https://{appConfiguration["HitomiServerInfoDomain"]}/gg.js";

        private LiveServerInfo? _liveServerInfo;
        private LiveServerInfo? LiveServerInfo {
            get => _liveServerInfo;
            set {
                _liveServerInfo = value;
                _lastLiveServerInfoUpdateTime = DateTime.UtcNow;
            }
        }
        private DateTime _lastLiveServerInfoUpdateTime = DateTime.MinValue;
        private readonly object _ggjsFetchLock = new();

        private readonly List<Downloader> _ggjsFetchWaiters = []; // note: _ggjsFetchWaiters is a subset of _liveDownloaders
        private readonly List<Downloader> _liveDownloaders = [];

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            ChannelReader<DownloadEventArgs> reader = eventBus.Subscribe();
            try {
                await foreach (DownloadEventArgs eventData in reader.ReadAllAsync(stoppingToken)) {
                    switch (eventData.DownloadRequest) {
                        case DownloadHubRequest.Start: {
                            if (eventData.GalleryId == default) {
                                throw new Exception($"GalleryId is required");
                            }
                            logger.LogInformation("{GalleryId}: Start request", eventData.GalleryId);
                            Downloader downloader = new(serviceProvider.CreateScope()) {
                                ConnectionId = eventData.ConnectionId,
                                GalleryId = eventData.GalleryId,
                                DownloadHubContext = hubContext,
                                LiveServerInfo = LiveServerInfo,
                                DownloadCompleted = HandleDownloadCompleted,
                                RequestGgjsFetch = WaitGgjsFetch
                            };
                            StartDownloader(downloader);
                            break;
                        }
                        case DownloadHubRequest.Pause: {
                            logger.LogInformation("{ConnectionId}: Pause request", eventData.ConnectionId);
                            foreach (Downloader d in _liveDownloaders) {
                                if (d.ConnectionId == eventData.ConnectionId) {
                                    d.Pause();
                                    break;
                                }
                            }
                            break;
                        }
                        case DownloadHubRequest.Resume: {
                            logger.LogInformation("{ConnectionId}: Resume request", eventData.ConnectionId);
                            foreach (Downloader d in _liveDownloaders) {
                                if (d.ConnectionId == eventData.ConnectionId) {
                                    StartDownloader(d);
                                    break;
                                }
                            }
                            break;
                        }
                        case DownloadHubRequest.Remove: {
                            logger.LogInformation("{ConnectionId}: Disconnect request", eventData.ConnectionId);
                            foreach (Downloader d in _liveDownloaders) {
                                if (d.ConnectionId == eventData.ConnectionId) {
                                    RemoveDownloader(d);
                                    break;
                                }
                            }
                            break;
                        }
                    }
                }
            } catch (OperationCanceledException) {
            } catch (Exception ex) {
                Console.WriteLine(ex);
            }
        }

        private void RemoveDownloader(Downloader d) {
            if (d == null) {
                return;
            }
            using (IServiceScope scope = serviceProvider.CreateScope()) {
                HitomiContext dbContext = scope.ServiceProvider.GetRequiredService<HitomiContext>();
                ICollection<int> downloads = dbContext.DownloadConfigurations.First().Downloads;
                if (downloads.Contains(d.GalleryId)) {
                    downloads.Remove(d.GalleryId);
                    dbContext.SaveChanges();
                }
            }
            _liveDownloaders.Remove(d);
            _ggjsFetchWaiters.Remove(d);
            d.Dispose();
        }

        private void HandleDownloadCompleted(Downloader downloader) {
            RemoveDownloader(downloader);
            hubContext.Clients.Client(downloader.ConnectionId!).ReceiveStatus(DownloadStatus.Completed, "");
        }

        private async Task WaitGgjsFetch(Downloader downloader) {
            if (downloader.LastLiveServerInfoUpdateTime < _lastLiveServerInfoUpdateTime) {
                downloader.LiveServerInfo = LiveServerInfo;
                StartDownloader(downloader);
                return;
            }
            _ggjsFetchWaiters.Add(downloader);
            if (Monitor.TryEnter(_ggjsFetchLock)) {
                try {
                    LiveServerInfo = await GetLiveServerInfo();
                    foreach (Downloader d in _liveDownloaders) {
                        d.LiveServerInfo = LiveServerInfo;
                    }
                    foreach (Downloader d in _ggjsFetchWaiters) {
                        StartDownloader(d);
                    }
                } finally {
                    Monitor.Exit(_ggjsFetchLock);
                }
            }
        }

        private void StartDownloader(Downloader downloader) {
            _liveDownloaders.Add(downloader);
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
                IsAAContains = match.Groups[1].Value == "0"
            };
        }

        public bool IsDownloading(int galleryId) {
            foreach (Downloader d in _liveDownloaders) {
                if (d.GalleryId == galleryId) {
                    return true;
                }
            }
            return false;
        }
    }
}
