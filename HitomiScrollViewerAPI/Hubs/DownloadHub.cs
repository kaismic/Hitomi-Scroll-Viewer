using HitomiScrollViewerAPI.Download;
using HitomiScrollViewerData;
using HitomiScrollViewerData.DbContexts;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Primitives;

namespace HitomiScrollViewerAPI.Hubs {
    public class DownloadHub
        (
            HitomiContext dbContext,
            IEventBus<DownloadEventArgs> eventBus,
            ILogger<DownloadHub> logger,
            DownloadManagerService downloadManagerService
        ) : Hub<IDownloadClient> {
        public override Task OnConnectedAsync() {
            StringValues query = Context.GetHttpContext()!.Request.Query["galleryId"];
            if (query.Count == 0) {
                Clients.Caller.ReceiveStatus(DownloadStatus.Failed, "Missing query parameter \"galleryId\"");
                Context.Abort();
                return base.OnConnectedAsync();
            }
            int galleryId = int.Parse(query.First()!);
            if (downloadManagerService.IsDownloading(galleryId)) {
                Clients.Caller.ReceiveStatus(DownloadStatus.Failed, $"Gallery with id {galleryId} is already downloading");
                Context.Abort();
                return base.OnConnectedAsync();
            } else {
                ICollection<int> downloads = dbContext.DownloadConfigurations.First().Downloads;
                if (!downloads.Contains(galleryId)) {
                    downloads.Add(galleryId);
                    dbContext.SaveChanges();
                }
            }
            logger.LogInformation("Connection created with gallery id {GalleryId}", galleryId);
            eventBus.Publish(new() {
                DownloadRequest = DownloadHubRequest.Start,
                ConnectionId = Context.ConnectionId,
                GalleryId = galleryId
            });
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception) {
            return base.OnDisconnectedAsync(exception);
        }

        // These methods are called by HubConnections from the client
        public void Resume() {
            eventBus.Publish(new() {
                DownloadRequest = DownloadHubRequest.Resume,
                ConnectionId = Context.ConnectionId
            });
        }

        public void Pause() {
            eventBus.Publish(new() {
                DownloadRequest = DownloadHubRequest.Pause,
                ConnectionId = Context.ConnectionId
            });
        }

        public void Remove() {
            eventBus.Publish(new() {
                DownloadRequest = DownloadHubRequest.Remove,
                ConnectionId = Context.ConnectionId
            });
            Context.Abort();
        }
    }
}
