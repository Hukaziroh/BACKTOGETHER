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
        private bool shuttingDown;

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
            if (ignoreAllMessages || shuttingDown || IsDisposed || IsDeadSocket(result.SocketId)) return;

            if (epicToSocketIds.TryGetValue(result.RemoteUserId, out SocketId activeSocket))
            {
                if (!IsSameSocket(activeSocket, result.SocketId))
                {
                    string closedSocketName = result.SocketId?.SocketName ?? "null";
                    Debug.LogWarning($"[EpicTransport Server] 과거 유령 소켓({closedSocketName}) 끊김 이벤트를 무시합니다. (현재: {activeSocket.SocketName})");
                    return;
                }
            }
            else
            {
                // 이미 정리됐거나 이전 서버 인스턴스에서 온 종료 알림이다.
                return;
            }

            base.OnConnectFail(result);
        }

        protected override void OnNewConnection(OnIncomingConnectionRequestInfo result)
        {
            if (ignoreAllMessages || shuttingDown || IsDisposed) return;
            if (result.RemoteUserId == null || result.SocketId == null || IsDeadSocket(result.SocketId)) return;

            if (epicToSocketIds.TryGetValue(result.RemoteUserId, out SocketId previousSocket) &&
                !IsSameSocket(previousSocket, result.SocketId))
            {
                // 같은 사용자가 새 소켓으로 재접속하면 이전 Mirror/P2P 연결부터 정리한다.
                int disconnectedConnectionId = -1;
                if (epicToMirrorIds.TryGetValue(result.RemoteUserId, out int previousConnectionId))
                {
                    epicToMirrorIds.Remove(result.RemoteUserId);
                    disconnectedConnectionId = previousConnectionId;
                }

                epicToSocketIds.Remove(result.RemoteUserId);
                CloseP2PSessionWithUser(result.RemoteUserId, previousSocket);

                if (disconnectedConnectionId >= 0)
                {
                    OnDisconnected?.Invoke(disconnectedConnectionId);
                }

                // Mirror disconnect callback에서 StopHost/Shutdown이 재진입할 수 있다.
                if (shuttingDown || IsDisposed) return;
            }

            // ACCEPT와 CONNECT 사이에 서버가 종료되는 경우도 닫을 수 있도록 pending 소켓부터 기록한다.
            epicToSocketIds[result.RemoteUserId] = result.SocketId;

            P2PInterface p2pInterface = EOSSDKComponent.GetP2PInterface();
            if (p2pInterface == null)
            {
                epicToSocketIds.Remove(result.RemoteUserId);
                return;
            }

            Result acceptResult = p2pInterface.AcceptConnection(new AcceptConnectionOptions
            {
                LocalUserId = EOSSDKComponent.LocalUserProductId,
                RemoteUserId = result.RemoteUserId,
                SocketId = result.SocketId
            });

            if (acceptResult != Result.Success)
            {
                epicToSocketIds.Remove(result.RemoteUserId);
                Debug.LogWarning($"[EpicTransport Server] P2P 연결 수락 실패 | Socket={result.SocketId.SocketName} | Result={acceptResult}");
            }
        }

        protected override void OnReceiveInternalData(InternalMessages type, ProductUserId clientUserId, SocketId socketId)
        {
            if (ignoreAllMessages || shuttingDown || IsDisposed) return;
            if (!epicToSocketIds.TryGetValue(clientUserId, out SocketId acceptedSocket) ||
                !IsSameSocket(acceptedSocket, socketId))
            {
                Debug.LogWarning("[EpicTransport Server] 수락되지 않았거나 과거 소켓의 내부 패킷을 무시합니다.");
                return;
            }

            switch (type)
            {
                case InternalMessages.CONNECT:
                    if (epicToMirrorIds.Count >= maxConnections && !epicToMirrorIds.Contains(clientUserId))
                    {
                        SendInternal(clientUserId, socketId, InternalMessages.DISCONNECT);
                        epicToSocketIds.Remove(clientUserId);
                        CloseP2PSessionWithUser(clientUserId, socketId);
                        return;
                    }

                    // CONNECT 재전송은 ACCEPT만 다시 보내고 Mirror 연결을 중복 생성하지 않는다.
                    if (epicToMirrorIds.Contains(clientUserId))
                    {
                        SendInternal(clientUserId, socketId, InternalMessages.ACCEPT_CONNECT);
                        return;
                    }

                    SendInternal(clientUserId, socketId, InternalMessages.ACCEPT_CONNECT);
                    int connectionId = nextConnectionID++;
                    epicToMirrorIds.Add(clientUserId, connectionId);
                    OnConnected.Invoke(connectionId);
                    Debug.Log($"[EpicTransport Server] P2P 연결 승인 | ConnectionId={connectionId} | Socket={socketId.SocketName}");
                    break;
                case InternalMessages.DISCONNECT:
                    int disconnectedConnectionId = -1;
                    if (epicToMirrorIds.TryGetValue(clientUserId, out int connId))
                    {
                        disconnectedConnectionId = connId;
                        epicToMirrorIds.Remove(clientUserId);
                    }

                    epicToSocketIds.Remove(clientUserId);
                    CloseP2PSessionWithUser(clientUserId, socketId);

                    if (disconnectedConnectionId >= 0)
                    {
                        OnDisconnected.Invoke(disconnectedConnectionId);
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
                if (epicToSocketIds.TryGetValue(clientUserId, out SocketId socketId))
                {
                    epicToSocketIds.Remove(clientUserId);
                    CloseP2PSessionWithUser(clientUserId, socketId);
                }
            }
        }

        protected override bool IsExpectedDataSocket(ProductUserId remoteUserId, SocketId incomingSocket)
        {
            return !shuttingDown &&
                   !IsDisposed &&
                   epicToMirrorIds.Contains(remoteUserId) &&
                   epicToSocketIds.TryGetValue(remoteUserId, out SocketId activeSocket) &&
                   IsSameSocket(activeSocket, incomingSocket);
        }

        public void Disconnect(int connectionId)
        {
            if (epicToMirrorIds.TryGetValue(connectionId, out ProductUserId userId))
            {
                epicToSocketIds.TryGetValue(userId, out SocketId socketId);

                if (socketId != null)
                {
                    SendInternal(userId, socketId, InternalMessages.DISCONNECT);
                }

                epicToMirrorIds.Remove(userId);
                epicToSocketIds.Remove(userId);

                if (socketId != null)
                {
                    CloseP2PSessionWithUser(userId, socketId);
                }

                // EOS의 로컬 CloseConnection 알림은 dead socket으로 무시하므로,
                // Mirror 정리는 여기서 정확히 한 번 완료한다.
                OnDisconnected?.Invoke(connectionId);
            }
        }

        public void Shutdown()
        {
            if (shuttingDown) return;
            shuttingDown = true;
            ignoreAllMessages = true;

            List<KeyValuePair<ProductUserId, SocketId>> peerSockets =
                new List<KeyValuePair<ProductUserId, SocketId>>(epicToSocketIds);

            foreach (KeyValuePair<ProductUserId, SocketId> peer in peerSockets)
            {
                SendInternal(peer.Key, peer.Value, InternalMessages.DISCONNECT);
                epicToMirrorIds.Remove(peer.Key);
                epicToSocketIds.Remove(peer.Key);
                CloseP2PSessionWithUser(peer.Key, peer.Value);
            }

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

            if (remoteId == null) return;

            bool hadMirrorConnection = epicToMirrorIds.TryGetValue(remoteId, out int connectionId);
            if (hadMirrorConnection)
            {
                epicToMirrorIds.Remove(remoteId);
            }

            epicToSocketIds.Remove(remoteId);

            if (hadMirrorConnection)
            {
                OnDisconnected.Invoke(connectionId);
            }
        }
    }
}
