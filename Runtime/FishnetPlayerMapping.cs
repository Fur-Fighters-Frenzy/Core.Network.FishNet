#if FISHNET_ENABLED
using Validosik.Core.Network.Mapping;
using FishNet.Connection;

namespace Validosik.Core.Network.FishNet
{
    internal sealed class FishnetPlayerMapping
        : PlayerConnectionMapping<
            NetworkConnection,
            NetworkConnectionPlayerRegistry,
            TokenRegistry> { }

    internal sealed class NetworkConnectionPlayerRegistry
        : BytePlayerRegistry<NetworkConnection> { }
}
#endif