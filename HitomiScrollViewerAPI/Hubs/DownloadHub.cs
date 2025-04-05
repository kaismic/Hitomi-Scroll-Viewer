using HitomiScrollViewerAPI.Download;
using HitomiScrollViewerData;
using HitomiScrollViewerData.DbContexts;
using HitomiScrollViewerData.Entities;
using Microsoft.AspNetCore.SignalR;

namespace HitomiScrollViewerAPI.Hubs {
    public class DownloadHub(HitomiContext context, IEventBus<DownloadEventArgs> eventBus) : Hub<IDownloadClient> {
        public override Task OnConnectedAsync() {
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
                eventBus.Publish(new DownloadEventArgs {
                    GalleryId = galleryId,
                    ConnectionId = Context.ConnectionId
                });
            }
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception) {
            return base.OnDisconnectedAsync(exception);
        }
    }
}
