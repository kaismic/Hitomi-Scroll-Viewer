using HitomiScrollViewerAPI.Services;
using HitomiScrollViewerData;
using Microsoft.AspNetCore.SignalR;

namespace HitomiScrollViewerAPI.Hubs {
    public class DbInitializeHub : Hub<IDbStatusClient> {
        public override async Task OnConnectedAsync() {
            await base.OnConnectedAsync();
            if (DbInitializeService.IsInitialized) {
                await Clients.Caller.ReceiveStatus(DbInitStatus.Complete, "");
            }
        }

        public override Task OnDisconnectedAsync(Exception? exception) {
            return base.OnDisconnectedAsync(exception);
        }
    }
}
