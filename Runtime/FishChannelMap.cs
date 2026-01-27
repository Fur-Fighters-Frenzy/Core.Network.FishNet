#if FISHNET_ENABLED
using Validosik.Core.Network.Transport;

namespace Validosik.Core.Network.FishNet
{
    public static class FishChannelMap
    {
        public static global::FishNet.Transporting.Channel ToFish(ChannelKind ch)
            => ch == ChannelKind.Unreliable
                ? global::FishNet.Transporting.Channel.Unreliable
                : global::FishNet.Transporting.Channel.Reliable;

        public static ChannelKind FromFish(global::FishNet.Transporting.Channel ch)
            => ch == global::FishNet.Transporting.Channel.Unreliable
                ? ChannelKind.Unreliable
                : ChannelKind.ReliableOrdered;
    }
}
#endif