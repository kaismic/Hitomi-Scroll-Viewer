using HitomiScrollViewerAPI.Hubs;
using HitomiScrollViewerAPI.Services;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using System.Threading.Channels;

namespace HitomiScrollViewerAPI.Download {
    // This class needs to be thread-safe
    public class DownloadService
        (
            IEventBus<DownloadEventArgs> eventBus,
            IHubContext<DownloadHub, IDownloadClient> hubContext,
            HttpClient httpClient,
            HitomiUrlService hitomiUrlService
        ) : BackgroundService {

        private const string GALLERY_INFO_EXCLUDE_STRING = "var galleryinfo = ";
        private const int SERVER_TIME_EXCLUDE_LENGTH = 16; // length of the string "0123456789/'\r\n};"

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

        private readonly ConcurrentQueue<Downloader> _ggjsFetchWaitQueue = [];
        private readonly ConcurrentQueue<Downloader> _pendingQueue = [];
        private readonly ConcurrentDictionary<string, Downloader> _liveDownloaders = [];

        private bool _isParallel;
        public bool IsParallel {
            get => _isParallel;
            set => Interlocked.Exchange(ref _isParallel, value);
        }
        private int _parallelThreadNum = 1;
        public int ParallelThreadNum {
            get => _parallelThreadNum;
            set => Interlocked.Exchange(ref _parallelThreadNum, value);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            ChannelReader<DownloadEventArgs> reader = eventBus.Subscribe();
            try {
                await foreach (DownloadEventArgs eventData in reader.ReadAllAsync(stoppingToken)) {
                    LiveServerInfo ??= await GetLiveServerInfo();
                    Downloader downloader = new() {
                        ConnectionId = eventData.ConnectionId,
                        GalleryId = eventData.GalleryId,
                        DownloadHubContext = hubContext,
                        LiveServerInfo = LiveServerInfo,
                        HttpClient = httpClient,
                        DownloadCompleted = HandleDownloadCompleted,
                        Http404ErrorLimitReached = HandleHttp404ErrorLimitReached
                    };
                    if (IsParallel || _liveDownloaders.IsEmpty && _pendingQueue.IsEmpty) {
                        StartDownload(downloader);
                    } else {
                        _pendingQueue.Enqueue(downloader);
                    }
                }
            } catch (OperationCanceledException) {
            } catch (Exception ex) {
                Console.WriteLine(ex);
            }
        }

        private void HandleDownloadCompleted(Downloader downloader) {
            downloader.Dispose();
            _liveDownloaders.TryRemove(downloader.ConnectionId, out _);
            if (!IsParallel && _liveDownloaders.IsEmpty) {
                if (_pendingQueue.TryDequeue(out Downloader? next)) {
                    StartDownload(next);
                }
            }
        }

        private async Task HandleHttp404ErrorLimitReached(Downloader downloader) {
            _ggjsFetchWaitQueue.Enqueue(downloader);
            if (Monitor.TryEnter(_ggjsFetchLock)) {
                try {
                    LiveServerInfo = await GetLiveServerInfo();
                    while (!_ggjsFetchWaitQueue.IsEmpty) {
                        if (_ggjsFetchWaitQueue.TryDequeue(out Downloader? next)) {
                            _ = next.Start();
                        }
                    }
                } finally {
                    Monitor.Exit(_ggjsFetchLock);
                }
            }
        }

        private void StartDownload(Downloader downloader) {
            _liveDownloaders.TryAdd(downloader.ConnectionId, downloader);
            _ = downloader.Start();
        }

        public void ResumeDownload(string connectionId) {
            _liveDownloaders.TryGetValue(connectionId, out Downloader? downloader);
            _ = downloader?.Start();
        }

        public void PauseDownload(string connectionId) {
            _liveDownloaders.TryGetValue(connectionId, out Downloader? downloader);
            downloader?.Pause();
        }
        
        public void RemoveDownload(string connectionId) {
            _liveDownloaders.TryGetValue(connectionId, out Downloader? downloader);
            downloader?.Remove();
            downloader?.Dispose();
            _liveDownloaders.TryRemove(connectionId, out _);
        }


        private async Task<LiveServerInfo> GetLiveServerInfo() {
            HttpResponseMessage response = await httpClient.GetAsync(hitomiUrlService.HitomiGgjsAddress);
            response.EnsureSuccessStatusCode();

            string content = await response.Content.ReadAsStringAsync();

            string serverTime = content.Substring(content.Length - SERVER_TIME_EXCLUDE_LENGTH, 10);
            string selectionSetPat = @"case (\d+)";
            MatchCollection matches = Regex.Matches(content, selectionSetPat);
            HashSet<string> subdomainSelectionSet = matches.Select(match => match.Groups[1].Value).ToHashSet();

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
