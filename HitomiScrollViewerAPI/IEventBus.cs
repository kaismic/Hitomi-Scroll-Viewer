using System.Threading.Channels;

namespace HitomiScrollViewerAPI {
    public interface IEventBus<T> {
        void Publish(T eventData);
        ChannelReader<T> Subscribe();
    }
}
