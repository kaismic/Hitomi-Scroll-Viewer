using HitomiScrollViewerAPI.Services;
using HitomiScrollViewerData;
using Microsoft.AspNetCore.SignalR;

namespace HitomiScrollViewerAPI.Hubs {
    public class DbStatusHub : Hub<IStatusClient> {
        public override async Task OnConnectedAsync() {
            await base.OnConnectedAsync();
            if (DbInitializeService.IsInitialized) {
                await Clients.Caller.ReceiveStatus(DbInitStatus.Complete, -1);
            }
        }

        public override Task OnDisconnectedAsync(Exception? exception) {
            return base.OnDisconnectedAsync(exception);
        }

        
    }
}
