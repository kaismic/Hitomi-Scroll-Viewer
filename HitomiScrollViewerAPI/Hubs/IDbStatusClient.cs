using HitomiScrollViewerData;

namespace HitomiScrollViewerAPI.Hubs {
    public interface IDbStatusClient {
        Task ReceiveStatus(DbInitStatus status, string message);
    }
}
