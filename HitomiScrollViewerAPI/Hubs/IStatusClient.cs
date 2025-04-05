using HitomiScrollViewerData;

namespace HitomiScrollViewerAPI.Hubs {
    public interface IStatusClient {
        Task ReceiveStatus(DbInitStatus status, int progress);
    }
}
