using HitomiScrollViewerAPI.Download;
using HitomiScrollViewerData;
using HitomiScrollViewerData.DbContexts;
using HitomiScrollViewerData.Entities;
using Microsoft.AspNetCore.SignalR;

namespace HitomiScrollViewerAPI.Hubs {
    public class DownloadHub(HitomiContext context, IEventBus<DownloadEventArgs> eventBus) : Hub<IDownloadClient> {
        public override Task OnConnectedAsync() {
            int configId = (int)Context.Items["ConfigId"]!;
            DownloadConfiguration? config = context.DownloadConfigurations.Find(configId);
            if (config == null) {
                Clients.Caller.ReceiveStatus(DownloadStatus.Failed, $"Configuration with id {configId} does not exist");
                Context.Abort();
                return base.OnConnectedAsync();
            }
            int galleryId = (int)Context.Items["GalleryId"]!;
            Gallery? gallery = context.Galleries.Find(galleryId);
            DownloadItem? downloadItem = context.DownloadItems.FirstOrDefault(d => d.GalleryId == galleryId);
            if (gallery != null || downloadItem != null) {
                string message = downloadItem != null ?
                    $"Gallery with id {galleryId} is already downloading" :
                    $"Gallery with id {galleryId} already exists";
                Clients.Caller.ReceiveStatus(DownloadStatus.Failed, message);
                Context.Abort();
            } else {
                downloadItem = new() { GalleryId = galleryId };
                config.DownloadItems.Add(downloadItem);
                context.SaveChanges();
                eventBus.Publish(new DownloadEventArgs {
                    DownloadRequest = DownloadRequest.Start,
                    ConnectionId = Context.ConnectionId,
                    DownloadItem = downloadItem.ToDTO()
                });
            }
            return base.OnConnectedAsync();
        }
        public override Task OnDisconnectedAsync(Exception? exception) {
            return base.OnDisconnectedAsync(exception);
        }

        // These methods are called by HubConnections from the client
        public void Resume() {
            eventBus.Publish(new DownloadEventArgs {
                DownloadRequest = DownloadRequest.Resume,
                ConnectionId = Context.ConnectionId
            });
        }

        public void Pause() {
            eventBus.Publish(new DownloadEventArgs {
                DownloadRequest = DownloadRequest.Pause,
                ConnectionId = Context.ConnectionId
            });
        }

        public void Remove() {
            eventBus.Publish(new DownloadEventArgs {
                DownloadRequest = DownloadRequest.Remove,
                ConnectionId = Context.ConnectionId
            });
        }
    }
}
