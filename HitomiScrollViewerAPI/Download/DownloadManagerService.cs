using HitomiScrollViewerAPI.Hubs;
using HitomiScrollViewerData;
using HitomiScrollViewerData.DbContexts;
using HitomiScrollViewerData.Entities;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using System.Threading.Channels;

namespace HitomiScrollViewerAPI.Download {
    public class DownloadManagerService
        (
            IEventBus<DownloadEventArgs> eventBus,
            IHubContext<DownloadHub, IDownloadClient> hubContext,
            IConfiguration appConfiguration,
            HttpClient httpClient
        ) : BackgroundService {
        private const int SERVER_TIME_EXCLUDE_LENGTH = 16; // length of the string "0123456789/'\r\n};"
        private readonly string _hitomiGgjsAddress = $"https://{appConfiguration["HitomiServerInfoDomain"]}/gg.js";

        private int _downloadConfigurationId;

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

        private readonly ConcurrentQueue<Downloader> _pendingQueue = [];
        private readonly List<Downloader> _ggjsFetchWaiters = []; // note: _ggjsFetchWaiters must be a subset of _liveDownloaders if my logic is correct
        private readonly List<Downloader> _liveDownloaders = [];

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            using (HitomiContext dbContext = new()) {
                _downloadConfigurationId = dbContext.DownloadConfigurations.First().Id;
            }
            ChannelReader<DownloadEventArgs> reader = eventBus.Subscribe();
            try {
                await foreach (DownloadEventArgs eventData in reader.ReadAllAsync(stoppingToken)) {
                    switch (eventData.DownloadRequest) {
                        case DownloadHubRequest.Start: {
                            using HitomiContext dbContext = new();
                            if (eventData.DownloadItem == null) {
                                throw new Exception($"{nameof(eventData.DownloadItem)} is null");
                            }
                            DownloadConfiguration config = dbContext.DownloadConfigurations.Find(_downloadConfigurationId)!;
                            Downloader downloader = new(appConfiguration, eventData.DownloadItem) {
                                ConnectionId = eventData.ConnectionId,
                                DownloadHubContext = hubContext,
                                LiveServerInfo = LiveServerInfo,
                                HttpClient = httpClient,
                                DownloadCompleted = HandleDownloadCompleted,
                                RequestGgjsFetch = WaitGgjsFetch
                            };
                            if (config.UseParallelDownload) {
                                StartDownload(downloader);
                                return;
                            }
                            if (_pendingQueue.IsEmpty) {
                                // check if any downloader's "Status" in _liveDownloders is "Downloading"
                                foreach (Downloader d in _liveDownloaders) {
                                    // if any downloader is downloading, add the current downloader to the pending queue
                                    if (d.Status == DownloadStatus.Downloading) {
                                        _pendingQueue.Enqueue(downloader);
                                        return;
                                    }
                                }
                            }
                            // at this point, ther are no downloading downloaders in _liveDownloaders so start the current downloader
                            StartDownload(downloader);
                            break;
                        }
                        case DownloadHubRequest.Pause: {
                            foreach (Downloader d in _liveDownloaders) {
                                if (d.ConnectionId == eventData.ConnectionId) {
                                    d.Pause();
                                    break;
                                }
                            }
                            break;
                        }
                        case DownloadHubRequest.Resume: {
                            foreach (Downloader d in _liveDownloaders) {
                                if (d.ConnectionId == eventData.ConnectionId) {
                                    StartDownload(d);
                                    break;
                                }
                            }
                            break;
                        }
                        case DownloadHubRequest.Disconnect: {
                            foreach (Downloader d in _liveDownloaders) {
                                if (d.ConnectionId == eventData.ConnectionId) {
                                    _liveDownloaders.Remove(d);
                                    _ggjsFetchWaiters.Remove(d);
                                    d.Dispose();
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

        private void HandleDownloadCompleted(Downloader downloader) {
            _liveDownloaders.Remove(downloader);
            downloader.Dispose();
            hubContext.Clients.Client(downloader.ConnectionId).ReceiveStatus(DownloadStatus.Completed, "");
            using HitomiContext dbContext = new();
            DownloadConfiguration config = dbContext.DownloadConfigurations.Find(_downloadConfigurationId)!;
            if (!config.UseParallelDownload) {
                if (_pendingQueue.TryDequeue(out Downloader? next)) {
                    StartDownload(next);
                }
            }
        }

        private async Task WaitGgjsFetch(Downloader downloader) {
            if (downloader.LastLiveServerInfoUpdateTime < _lastLiveServerInfoUpdateTime) {
                downloader.LiveServerInfo = LiveServerInfo;
                StartDownload(downloader);
                return;
            }
            _ggjsFetchWaiters.Add(downloader);
            if (Monitor.TryEnter(_ggjsFetchLock)) {
                try {
                    LiveServerInfo = await GetLiveServerInfo();
                    foreach (Downloader d in _pendingQueue) {
                        d.LiveServerInfo = LiveServerInfo;
                    }
                    foreach (Downloader d in _liveDownloaders) {
                        d.LiveServerInfo = LiveServerInfo;
                    }
                    foreach (Downloader d in _ggjsFetchWaiters) {
                        StartDownload(d);
                    }
                } finally {
                    Monitor.Exit(_ggjsFetchLock);
                }
            }
        }

        private void StartDownload(Downloader downloader) {
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
    }
}
