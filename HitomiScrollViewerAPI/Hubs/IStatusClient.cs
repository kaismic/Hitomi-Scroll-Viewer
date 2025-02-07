using HitomiScrollViewerData;

namespace HitomiScrollViewerAPI.Hubs {
    public interface IStatusClient {
        Task ReceiveStatus(InitStatus status, int progress);
    }
}
