#if FISHNET_ENABLED
using System;
using Validosik.Core.Network.Transport;
using Validosik.Core.Network.Transport.Interfaces;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Transporting;

namespace Validosik.Core.Network.FishNet
{
    /// Hooked on the client singleton object
    public partial class FishBridge : NetworkBehaviour, INetClient
    {
        public event Action<ReadOnlyMemory<byte>, ChannelKind> OnServerMessage;
        public event Action OnConnectedAsClient;

        /// Send: Client -> Serve
        [Client]
        public void Send(ReadOnlySpan<byte> raw, ChannelKind ch = ChannelKind.ReliableOrdered) =>
            Rpc_FromClient(raw.ToArray(), FishChannelMap.ToFish(ch));

        /// Receive: Server -> Client (Observers/Target)
        [TargetRpc]
        private void Rpc_ToClient(NetworkConnection connection, byte[] data, Channel channel = Channel.Reliable)
        {
            OnServerMessage?.Invoke(data, FishChannelMap.FromFish(channel));
        }

        /// Receive: Server -> to all Clients (Observers)
        [ObserversRpc]
        private void Rpc_ToAll(byte[] data, Channel channel = Channel.Reliable)
        {
            OnServerMessage?.Invoke(data, FishChannelMap.FromFish(channel));
        }

        /// Send: Client -> Serve
        [Client]
        public void SendHandshake() =>
            Rpc_HandshakeFromClient(Array.Empty<byte>());

        private void OnConnectedToServer()
        {
            OnConnectedAsClient?.Invoke();
        }
    }
}
#endif