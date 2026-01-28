#if FISHNET_ENABLED
using System;
using System.Buffers;
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
        public void Send(ReadOnlySpan<byte> raw, ChannelKind ch = ChannelKind.ReliableOrdered)
        {
            var rented = ArrayPool<byte>.Shared.Rent(raw.Length);
            try
            {
                raw.CopyTo(rented);
                Rpc_FromClient(new ArraySegment<byte>(rented, 0, raw.Length), FishChannelMap.ToFish(ch));
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rented);
            }
        }

        /// Receive: Server -> Client (Observers/Target)
        [TargetRpc]
        private void Rpc_ToClient(NetworkConnection connection, ArraySegment<byte> data, Channel channel = Channel.Reliable)
        {
            if (data.Array == null)
            {
                OnServerMessage?.Invoke(ReadOnlyMemory<byte>.Empty, FishChannelMap.FromFish(channel));
                return;
            }

            OnServerMessage?.Invoke(data.Array.AsMemory(data.Offset, data.Count), FishChannelMap.FromFish(channel));
        }

        /// Receive: Server -> to all Clients (Observers)
        [ObserversRpc]
        private void Rpc_ToAll(ArraySegment<byte> data, Channel channel = Channel.Reliable)
        {
            if (data.Array == null)
            {
                OnServerMessage?.Invoke(ReadOnlyMemory<byte>.Empty, FishChannelMap.FromFish(channel));
                return;
            }

            OnServerMessage?.Invoke(data.Array.AsMemory(data.Offset, data.Count), FishChannelMap.FromFish(channel));
        }

        /// Send: Client -> Serve
        [Client]
        public void SendHandshake() =>
            Rpc_HandshakeFromClient(new ArraySegment<byte>(Array.Empty<byte>()));

        private void OnConnectedToServer()
        {
            OnConnectedAsClient?.Invoke();
        }
    }
}
#endif