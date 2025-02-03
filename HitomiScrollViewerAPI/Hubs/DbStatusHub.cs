using Microsoft.AspNetCore.SignalR;

namespace HitomiScrollViewerAPI.Hubs {
    public class DbStatusHub : Hub<IStatusClient> {
        public override async Task OnConnectedAsync() {
            DatabaseInitializer.StatusChanged += DatabaseInitializer_StatusChanged;
            if (DatabaseInitializer.IsInitialized) {
                // If the database is already initialized, detach the event handler and send the complete status to the client
                DatabaseInitializer.StatusChanged -= DatabaseInitializer_StatusChanged;
                await Clients.All.ReceiveStatus(InitStatus.Complete, null);
            }
            await base.OnConnectedAsync();
        }

        private async void DatabaseInitializer_StatusChanged(InitStatus status, InitProgress? progress) {
            await Clients.All.ReceiveStatus(status, progress);
        }

        public override Task OnDisconnectedAsync(Exception? exception) {
            return base.OnDisconnectedAsync(exception);
        }
    }
}
