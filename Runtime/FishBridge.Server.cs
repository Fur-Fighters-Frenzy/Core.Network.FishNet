#if FISHNET_ENABLED
using System;
using System.Collections.Generic;
using Validosik.Core.Network.Dto;
using Validosik.Core.Network.Transport;
using Validosik.Core.Network.Transport.Interfaces;
using Validosik.Core.Network.Types;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Transporting;

namespace Validosik.Core.Network.FishNet
{
    /// Hooked on the server singleton object
    public partial class FishBridge : NetworkBehaviour, INetServer
    {
        public event Action<PlayerId, ReadOnlyMemory<byte>, ChannelKind> OnClientMessage;
        public event Action<PlayerId> OnClientDisconnected;
        public event Action<PlayerId> OnClientConnected;

        private FishnetPlayerMapping _registry;

        /// Send: Server -> one Client
        [Server]
        public void Send(PlayerId to, ReadOnlySpan<byte> raw, ChannelKind ch = ChannelKind.ReliableOrdered)
        {
            if (!_registry.TryGetConnection(to, out var connection))
            {
                return;
            }

            Rpc_ToClient(connection, raw.ToArray(), FishChannelMap.ToFish(ch));
        }

        /// Send: Server -> one Client
        [Server]
        private void SendRaw(NetworkConnection connection, ReadOnlySpan<byte> raw,
            ChannelKind ch = ChannelKind.ReliableOrdered)
        {
            if (connection == null)
            {
                return;
            }

            Rpc_ToClient(connection, raw.ToArray(), FishChannelMap.ToFish(ch));
        }

        /// Send: Server -> all Clients (Observers)
        [Server]
        public void Broadcast(ReadOnlySpan<byte> raw, ChannelKind ch = ChannelKind.ReliableOrdered)
        {
            Rpc_ToAll(raw.ToArray(), FishChannelMap.ToFish(ch));
        }

        [Server]
        public void BroadcastExcept(PlayerId except, ReadOnlySpan<byte> raw,
            ChannelKind ch = ChannelKind.ReliableOrdered)
        {
            var channel = FishChannelMap.ToFish(ch);
            var payload = raw.ToArray();

            foreach (var (connection, pid) in _registry.AllConnections)
            {
                if (pid == except)
                {
                    continue;
                }

                Rpc_ToClient(connection, payload, channel);
            }
        }

        [Server]
        public void BroadcastExcept(PlayerId[] except, ReadOnlySpan<byte> raw,
            ChannelKind ch = ChannelKind.ReliableOrdered)
        {
            if (except == null || except.Length == 0)
            {
                Broadcast(raw, ch);
                return;
            }

            // Convert to HashSet for fast lookup
            var skip = new HashSet<byte>();
            foreach (var pid in except)
            {
                skip.Add(pid.Value);
            }

            var channel = FishChannelMap.ToFish(ch);
            var payload = raw.ToArray();

            foreach (var (connection, pid) in _registry.AllConnections)
            {
                if (skip.Contains(pid))
                {
                    continue;
                }

                Rpc_ToClient(connection, payload, channel);
            }
        }

        public void Poll()
        {
            /* no-op */
        }

        public void Disconnect(PlayerId pid)
        {
            if (!_registry.TryGetConnection(pid, out var connection))
            {
                return;
            }

            connection.Disconnect(true); // TODO: maybe sending pending data here
        }

        /// Receive: Client -> Server
        [ServerRpc(RequireOwnership = false)]
        private void Rpc_FromClient(byte[] data, Channel channel = Channel.Reliable, NetworkConnection sender = null)
        {
            var (pid, _) = _registry.MapConnectionToPlayer(sender);
            OnClientMessage?.Invoke(pid, data, FishChannelMap.FromFish(channel));
        }

        /// Receive: Client -> Server
        [ServerRpc(RequireOwnership = false)]
        private void Rpc_HandshakeFromClient(byte[] _, NetworkConnection sender = null)
        {
            if (!_registry.TryGetPid(sender, out var pid)
                || !_registry.TryGetToken(pid, out var token))
            {
                return;
            }

            var handshake = new HandshakeDto(
                0,
                0,
                pid,
                token
            );
            var payload = handshake.ToBytes();
            var envelope = NetEnvelope.Pack((ushort)ServerMsgType.Handshake, payload);
            Send(pid, envelope);

            OnClientConnected?.Invoke(pid);
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            _registry = new FishnetPlayerMapping();

            if (base.NetworkManager == null)
            {
                return;
            }

            var sm = base.NetworkManager.ServerManager;
            if (sm != null)
            {
                // Fires for each client upon connect/disconnect
                sm.OnRemoteConnectionState += OnRemoteConnectionState;
            }
        }

        public override void OnStopServer()
        {
            if (base.NetworkManager != null)
            {
                var sm = base.NetworkManager.ServerManager;
                if (sm != null)
                {
                    sm.OnRemoteConnectionState -= OnRemoteConnectionState;
                }
            }

            _registry = null;
            base.OnStopServer();
        }

        private void OnRemoteConnectionState(NetworkConnection connection, RemoteConnectionStateArgs args)
        {
            // args.ConnectionState: Started / Stopped
            if (args.ConnectionState == RemoteConnectionState.Stopped)
            {
                var pid = _registry.ReleaseConnection(connection);
                if (pid != NetworkConnectionPlayerRegistry.None)
                {
                    OnClientDisconnected?.Invoke(pid);
                }
            }
            else if (args.ConnectionState == RemoteConnectionState.Started)
            {
                var (pid, token) = _registry.MapConnectionToPlayer(connection);
                // Sorry bro connection doesn't here anymore. Cause client subscribes to handshake after it sends here 
            }
        }

        public ushort Tick { get; private set; }

        public void NextTick()
        {
            unchecked
            {
                ++Tick;
            }
        }
    }
}
#endif