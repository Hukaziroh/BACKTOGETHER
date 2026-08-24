using Epic.OnlineServices;
using Epic.OnlineServices.P2P;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace EpicTransport
{
    public class Server : Common
    {
        private event Action<int> OnConnected;
        private event Action<int, byte[], int> OnReceivedData;
        private event Action<int> OnDisconnected;
        private event Action<int, Exception> OnReceivedError;

        private BidirectionalDictionary<ProductUserId, int> epicToMirrorIds;
        private Dictionary<ProductUserId, SocketId> epicToSocketIds;
        private int maxConnections;
        private int nextConnectionID;

        public static Server CreateServer(EosTransport transport, int maxConnections)
        {
            Server s = new Server(transport, maxConnections);
            s.OnConnected += (id) => transport.OnServerConnectedWithAddress.Invoke(id, "");
            s.OnDisconnected += (id) => transport.OnServerDisconnected.Invoke(id);
            s.OnReceivedData += (id, data, channel) => transport.OnServerDataReceived.Invoke(id, new ArraySegment<byte>(data), channel);
            s.OnReceivedError += (id, exception) => transport.OnServerError.Invoke(id, Mirror.TransportError.Unexpected, exception.ToString());
            return s;
        }

        private Server(EosTransport transport, int maxConnections) : base(transport)
        {
            this.maxConnections = maxConnections;
            epicToMirrorIds = new BidirectionalDictionary<ProductUserId, int>();
            epicToSocketIds = new Dictionary<ProductUserId, SocketId>();
            nextConnectionID = 1;
        }

        // ★ 핵심 방어막: 호스트 측에서도 과거 소켓의 찌꺼기 이벤트를 튕겨냅니다!
        protected override void OnConnectFail(OnRemoteConnectionClosedInfo result)
        {
            if (ignoreAllMessages) return;

            if (epicToSocketIds.TryGetValue(result.RemoteUserId, out SocketId activeSocket))
            {
                if (result.SocketId != null && activeSocket.SocketName != result.SocketId.SocketName)
                {
                    Debug.LogWarning($"[EpicTransport Server] 과거 유령 소켓({result.SocketId.SocketName}) 끊김 이벤트를 무시합니다. (현재: {activeSocket.SocketName})");
                    return;
                }
            }
            base.OnConnectFail(result);
        }

        protected override void OnNewConnection(OnIncomingConnectionRequestInfo result)
        {
            if (ignoreAllMessages) return;
            if (deadSockets.Contains(result.SocketId.SocketName)) return;

            EOSSDKComponent.GetP2PInterface().AcceptConnection(new AcceptConnectionOptions() { LocalUserId = EOSSDKComponent.LocalUserProductId, RemoteUserId = result.RemoteUserId, SocketId = result.SocketId });
        }

        protected override void OnReceiveInternalData(InternalMessages type, ProductUserId clientUserId, SocketId socketId)
        {
            if (ignoreAllMessages) return;

            switch (type)
            {
                case InternalMessages.CONNECT:
                    if (epicToMirrorIds.Count >= maxConnections && !epicToMirrorIds.ContainsKey(clientUserId)) { SendInternal(clientUserId, socketId, InternalMessages.DISCONNECT); return; }
                    SendInternal(clientUserId, socketId, InternalMessages.ACCEPT_CONNECT);
                    int connectionId = nextConnectionID++;
                    if (epicToMirrorIds.ContainsKey(clientUserId))
                    {
                        epicToMirrorIds.Remove(clientUserId);
                        epicToSocketIds.Remove(clientUserId);
                    }
                    epicToMirrorIds.Add(clientUserId, connectionId);
                    epicToSocketIds.Add(clientUserId, socketId);
                    OnConnected.Invoke(connectionId);
                    break;
                case InternalMessages.DISCONNECT:
                    if (epicToMirrorIds.TryGetValue(clientUserId, out int connId))
                    {
                        OnDisconnected.Invoke(connId);
                        epicToMirrorIds.Remove(clientUserId);
                        epicToSocketIds.Remove(clientUserId);
                    }
                    break;
            }
        }

        protected override void OnReceiveData(byte[] data, ProductUserId clientUserId, int channel)
        {
            if (ignoreAllMessages) return;

            if (epicToMirrorIds.TryGetValue(clientUserId, out int connectionId))
            {
                OnReceivedData.Invoke(connectionId, data, channel);
            }
            else
            {
                SocketId socketId;
                epicToSocketIds.TryGetValue(clientUserId, out socketId);
                CloseP2PSessionWithUser(clientUserId, socketId);
            }
        }

        public void Disconnect(int connectionId)
        {
            if (epicToMirrorIds.TryGetValue(connectionId, out ProductUserId userId))
            {
                SocketId socketId;
                epicToSocketIds.TryGetValue(userId, out socketId);
                SendInternal(userId, socketId, InternalMessages.DISCONNECT);
                epicToMirrorIds.Remove(userId);
                epicToSocketIds.Remove(userId);
            }
        }

        public void Shutdown()
        {
            List<int> connectionIds = new List<int>();
            for (int connectionId = 1; connectionId < nextConnectionID; connectionId++)
            {
                if (epicToMirrorIds.TryGetValue(connectionId, out ProductUserId userId)) connectionIds.Add(connectionId);
            }
            foreach (int connectionId in connectionIds)
            {
                if (!epicToMirrorIds.TryGetValue(connectionId, out ProductUserId userId)) continue;
                SocketId socketId = null;
                if (epicToSocketIds.TryGetValue(userId, out SocketId foundSocket)) socketId = foundSocket;
                Disconnect(connectionId);
                if (socketId != null) WaitForClose(userId, socketId);
            }
            ignoreAllMessages = true;
            try { ReceiveData(); } catch (Exception e) { Debug.LogException(e); }
            Dispose();
        }

        public void SendAll(int connectionId, byte[] data, int channelId)
        {
            if (epicToMirrorIds.TryGetValue(connectionId, out ProductUserId userId))
            {
                SocketId socketId;
                epicToSocketIds.TryGetValue(userId, out socketId);
                Send(userId, socketId, data, (byte)channelId);
            }
        }

        public string ServerGetClientAddress(int connectionId)
        {
            if (epicToMirrorIds.TryGetValue(connectionId, out ProductUserId userId))
            {
                string userIdString; userId.ToString(out userIdString); return userIdString;
            }
            return string.Empty;
        }

        protected override void OnConnectionFailed(ProductUserId remoteId)
        {
            if (ignoreAllMessages) return;

            int connectionId = epicToMirrorIds.TryGetValue(remoteId, out int connId) ? connId : nextConnectionID++;
            OnDisconnected.Invoke(connectionId);
            epicToMirrorIds.Remove(remoteId);
            epicToSocketIds.Remove(remoteId);
        }
    }
}