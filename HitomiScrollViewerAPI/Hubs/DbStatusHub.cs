using Microsoft.AspNetCore.SignalR;

namespace HitomiScrollViewerAPI.Hubs {
    public class DbStatusHub : Hub {
        public async Task SendStatus(string status) {
            await Clients.All.SendAsync("StatusReceived", status);
        }

        public override async Task OnConnectedAsync() {
            // TODO: start dabase initialization
            await SendStatus("Initializing database...");
            await Task.Delay(3000);
            await SendStatus("222");
            await Task.Delay(3000);
            await SendStatus("333");
            await base.OnConnectedAsync();
        }
    }
}
