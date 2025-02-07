﻿using HitomiScrollViewerData;
using Microsoft.AspNetCore.SignalR;

namespace HitomiScrollViewerAPI.Hubs {
    public class DbStatusHub : Hub<IStatusClient> {
        public override async Task OnConnectedAsync() {
            if (DatabaseInitializer.IsInitialized) {
                await Clients.All.ReceiveStatus(InitStatus.Complete, -1);
            }
            await base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception) {
            return base.OnDisconnectedAsync(exception);
        }
    }
}
