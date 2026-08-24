using Epic.OnlineServices;
using Epic.OnlineServices.P2P;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace EpicTransport
{
    public abstract class Common
    {

        private PacketReliability[] channels;
        private int internal_ch => channels.Length;

        protected enum InternalMessages : byte
        {
            CONNECT,
            ACCEPT_CONNECT,
            DISCONNECT
        }

        protected struct PacketKey
        {
            public ProductUserId productUserId;
            public byte channel;
            public string socketName;
        }

        private OnIncomingConnectionRequestCallback OnIncomingConnectionRequest;
        ulong incomingNotificationId = 0;
        private OnRemoteConnectionClosedCallback OnRemoteConnectionClosed;
        ulong outgoingNotificationId = 0;
        private bool disposed;

        protected readonly EosTransport transport;
        protected List<string> deadSockets;
        public bool ignoreAllMessages = false;
        protected Dictionary<PacketKey, List<List<Packet>>> incomingPackets = new Dictionary<PacketKey, List<List<Packet>>>();

        protected Common(EosTransport transport)
        {
            channels = transport.Channels;
            deadSockets = new List<string>();

            AddNotifyPeerConnectionRequestOptions addNotifyPeerConnectionRequestOptions = new AddNotifyPeerConnectionRequestOptions();
            addNotifyPeerConnectionRequestOptions.LocalUserId = EOSSDKComponent.LocalUserProductId;
            addNotifyPeerConnectionRequestOptions.SocketId = null;

            OnIncomingConnectionRequest += OnNewConnection;
            OnRemoteConnectionClosed += OnConnectFail;

            incomingNotificationId = EOSSDKComponent.GetP2PInterface().AddNotifyPeerConnectionRequest(addNotifyPeerConnectionRequestOptions, null, OnIncomingConnectionRequest);

            AddNotifyPeerConnectionClosedOptions addNotifyPeerConnectionClosedOptions = new AddNotifyPeerConnectionClosedOptions();
            addNotifyPeerConnectionClosedOptions.LocalUserId = EOSSDKComponent.LocalUserProductId;
            addNotifyPeerConnectionClosedOptions.SocketId = null;

            outgoingNotificationId = EOSSDKComponent.GetP2PInterface().AddNotifyPeerConnectionClosed(addNotifyPeerConnectionClosedOptions, null, OnRemoteConnectionClosed);

            if (outgoingNotificationId == 0 || incomingNotificationId == 0)
            {
                Debug.LogError("Couldn't bind notifications with P2P interface");
            }
            incomingPackets = new Dictionary<PacketKey, List<List<Packet>>>();
            this.transport = transport;
        }

        protected void Dispose()
        {
            if (disposed) return;
            disposed = true;

            P2PInterface p2pInterface = EOSSDKComponent.GetP2PInterface();
            if (p2pInterface != null)
            {
                if (incomingNotificationId != 0)
                {
                    p2pInterface.RemoveNotifyPeerConnectionRequest(incomingNotificationId);
                    incomingNotificationId = 0;
                }

                if (outgoingNotificationId != 0)
                {
                    p2pInterface.RemoveNotifyPeerConnectionClosed(outgoingNotificationId);
                    outgoingNotificationId = 0;
                }
            }

            transport.ResetIgnoreMessagesAtStartUpTimer();
        }

        protected bool IsDisposed => disposed;

        protected abstract void OnNewConnection(OnIncomingConnectionRequestInfo result);

        // ★ 1. 자식에서 오버라이드할 수 있게 protected virtual로 수정
        protected virtual void OnConnectFail(OnRemoteConnectionClosedInfo result)
        {
            if (ignoreAllMessages) return;
            if (IsDeadSocket(result.SocketId)) return;

            OnConnectionFailed(result.RemoteUserId);

            // ★ 2. 엔진 크래시/먹통을 유발하는 throw new Exception 제거
            switch (result.Reason)
            {
                case ConnectionClosedReason.ClosedByLocalUser:
                    Debug.LogWarning("Connection closed: The Connection was gracefully closed by the local user."); break;
                case ConnectionClosedReason.ClosedByPeer:
                    Debug.LogWarning("Connection closed: The connection was gracefully closed by remote user."); break;
                case ConnectionClosedReason.ConnectionClosed:
                    Debug.LogWarning("Connection closed: The connection was unexpectedly closed."); break;
                case ConnectionClosedReason.ConnectionFailed:
                    Debug.LogWarning("Connection failed: Failed to establish connection."); break;
                case ConnectionClosedReason.InvalidData:
                    Debug.LogWarning("Connection failed: The remote user sent us invalid data."); break;
                case ConnectionClosedReason.InvalidMessage:
                    Debug.LogWarning("Connection failed: The remote user sent us an invalid message."); break;
                case ConnectionClosedReason.NegotiationFailed:
                    Debug.LogWarning("Connection failed: Negotiation failed."); break;
                case ConnectionClosedReason.TimedOut:
                    Debug.LogWarning("Connection failed: Timeout."); break;
                case ConnectionClosedReason.TooManyConnections:
                    Debug.LogWarning("Connection failed: Too many connections."); break;
                case ConnectionClosedReason.UnexpectedError:
                    Debug.LogWarning("Unexpected Error, connection will be closed"); break;
                case ConnectionClosedReason.Unknown:
                default:
                    Debug.LogWarning("Unknown Error, connection has been closed."); break;
            }
        }

        protected void SendInternal(ProductUserId target, SocketId socketId, InternalMessages type)
        {
            if (target == null || socketId == null) return;

            P2PInterface p2pInterface = EOSSDKComponent.GetP2PInterface();
            if (p2pInterface == null) return;

            Result result = p2pInterface.SendPacket(new SendPacketOptions()
            {
                AllowDelayedDelivery = true,
                Channel = (byte)internal_ch,
                Data = new byte[] { (byte)type },
                LocalUserId = EOSSDKComponent.LocalUserProductId,
                Reliability = PacketReliability.ReliableOrdered,
                RemoteUserId = target,
                SocketId = socketId
            });

            if (result != Result.Success)
            {
                Debug.LogWarning($"[EpicTransport] {type} 전송 실패: {result}");
            }
        }

        protected void Send(ProductUserId host, SocketId socketId, byte[] msgBuffer, byte channel)
        {
            Result result = EOSSDKComponent.GetP2PInterface().SendPacket(new SendPacketOptions()
            {
                AllowDelayedDelivery = true,
                Channel = channel,
                Data = msgBuffer,
                LocalUserId = EOSSDKComponent.LocalUserProductId,
                Reliability = channels[channel],
                RemoteUserId = host,
                SocketId = socketId
            });
            if (result != Result.Success) Debug.LogError("Send failed " + result);
        }

        private bool Receive(out ProductUserId clientProductUserId, out SocketId socketId, out byte[] receiveBuffer, byte channel)
        {
            Result result = EOSSDKComponent.GetP2PInterface().ReceivePacket(new ReceivePacketOptions()
            {
                LocalUserId = EOSSDKComponent.LocalUserProductId,
                MaxDataSizeBytes = P2PInterface.MaxPacketSize,
                RequestedChannel = channel
            }, out clientProductUserId, out socketId, out channel, out receiveBuffer);
            if (result == Result.Success) return true;
            receiveBuffer = null; clientProductUserId = null; return false;
        }

        protected virtual void CloseP2PSessionWithUser(ProductUserId clientUserID, SocketId socketId)
        {
            if (clientUserID == null || socketId == null || string.IsNullOrEmpty(socketId.SocketName)) return;
            if (deadSockets == null || deadSockets.Contains(socketId.SocketName)) return;

            P2PInterface p2pInterface = EOSSDKComponent.GetP2PInterface();
            if (p2pInterface == null)
            {
                Debug.LogWarning("[EpicTransport] EOS P2P 인터페이스가 없어 연결을 닫지 못했습니다.");
                return;
            }

            deadSockets.Add(socketId.SocketName);
            DiscardIncomingPackets(clientUserID, socketId.SocketName);

            Result result = p2pInterface.CloseConnection(new CloseConnectionOptions
            {
                LocalUserId = EOSSDKComponent.LocalUserProductId,
                RemoteUserId = clientUserID,
                SocketId = socketId
            });

            if (result != Result.Success && result != Result.NoConnection)
            {
                deadSockets.Remove(socketId.SocketName);
                Debug.LogWarning($"[EpicTransport] P2P 연결 종료 실패 | Socket={socketId.SocketName} | Result={result}");
                return;
            }

            Debug.Log($"[EpicTransport] P2P 연결 종료 완료 | Socket={socketId.SocketName}");
        }

        private void DiscardIncomingPackets(ProductUserId remoteUserId, string socketName)
        {
            if (incomingPackets.Count == 0) return;

            List<PacketKey> stalePacketKeys = new List<PacketKey>();
            foreach (PacketKey packetKey in incomingPackets.Keys)
            {
                if (packetKey.productUserId == remoteUserId && packetKey.socketName == socketName)
                {
                    stalePacketKeys.Add(packetKey);
                }
            }

            foreach (PacketKey packetKey in stalePacketKeys)
            {
                incomingPackets.Remove(packetKey);
            }
        }

        protected bool IsDeadSocket(SocketId socketId)
        {
            return socketId != null &&
                   !string.IsNullOrEmpty(socketId.SocketName) &&
                   deadSockets != null &&
                   deadSockets.Contains(socketId.SocketName);
        }

        protected static bool IsSameSocket(SocketId left, SocketId right)
        {
            return left != null &&
                   right != null &&
                   !string.IsNullOrEmpty(left.SocketName) &&
                   left.SocketName == right.SocketName;
        }

        public void ReceiveData()
        {
            try
            {
                SocketId socketId = new SocketId();
                while (transport.enabled && Receive(out ProductUserId clientUserID, out socketId, out byte[] internalMessage, (byte)internal_ch))
                {
                    if (internalMessage.Length == 1)
                    {
                        OnReceiveInternalData((InternalMessages)internalMessage[0], clientUserID, socketId);
                        return;
                    }
                }
                for (int chNum = 0; chNum < channels.Length; chNum++)
                {
                    while (transport.enabled && Receive(out ProductUserId clientUserID, out socketId, out byte[] receiveBuffer, (byte)chNum))
                    {
                        if (!IsExpectedDataSocket(clientUserID, socketId))
                        {
                            continue;
                        }

                        PacketKey incomingPacketKey = new PacketKey()
                        {
                            productUserId = clientUserID,
                            channel = (byte)chNum,
                            socketName = socketId.SocketName
                        };
                        Packet packet = new Packet(); packet.FromBytes(receiveBuffer);
                        if (!incomingPackets.ContainsKey(incomingPacketKey)) incomingPackets.Add(incomingPacketKey, new List<List<Packet>>());
                        int packetListIndex = incomingPackets[incomingPacketKey].Count;
                        for (int i = 0; i < incomingPackets[incomingPacketKey].Count; i++)
                        {
                            if (incomingPackets[incomingPacketKey][i][0].id == packet.id) { packetListIndex = i; break; }
                        }
                        if (packetListIndex == incomingPackets[incomingPacketKey].Count) incomingPackets[incomingPacketKey].Add(new List<Packet>());
                        int insertionIndex = -1;
                        for (int i = 0; i < incomingPackets[incomingPacketKey][packetListIndex].Count; i++)
                        {
                            if (incomingPackets[incomingPacketKey][packetListIndex][i].fragment > packet.fragment) { insertionIndex = i; break; }
                        }
                        if (insertionIndex >= 0) incomingPackets[incomingPacketKey][packetListIndex].Insert(insertionIndex, packet);
                        else incomingPackets[incomingPacketKey][packetListIndex].Add(packet);
                    }
                }
                List<List<Packet>> emptyPacketLists = new List<List<Packet>>();
                foreach (KeyValuePair<PacketKey, List<List<Packet>>> keyValuePair in incomingPackets)
                {
                    for (int packetList = 0; packetList < keyValuePair.Value.Count; packetList++)
                    {
                        bool packetReady = true; int packetLength = 0;
                        for (int packet = 0; packet < keyValuePair.Value[packetList].Count; packet++)
                        {
                            Packet tempPacket = keyValuePair.Value[packetList][packet];
                            if (tempPacket.fragment != packet || (packet == keyValuePair.Value[packetList].Count - 1 && tempPacket.moreFragments)) packetReady = false;
                            else packetLength += tempPacket.data.Length;
                        }
                        if (packetReady)
                        {
                            byte[] data = new byte[packetLength]; int dataIndex = 0;
                            for (int packet = 0; packet < keyValuePair.Value[packetList].Count; packet++)
                            {
                                Array.Copy(keyValuePair.Value[packetList][packet].data, 0, data, dataIndex, keyValuePair.Value[packetList][packet].data.Length);
                                dataIndex += keyValuePair.Value[packetList][packet].data.Length;
                            }
                            OnReceiveData(data, keyValuePair.Key.productUserId, keyValuePair.Key.channel);
                            if (transport.ServerActive() || transport.ClientActive()) emptyPacketLists.Add(keyValuePair.Value[packetList]);
                        }
                    }
                    for (int i = 0; i < emptyPacketLists.Count; i++) keyValuePair.Value.Remove(emptyPacketLists[i]);
                    emptyPacketLists.Clear();
                }
            }
            catch (Exception e) { Debug.LogException(e); }
        }
        protected abstract void OnReceiveInternalData(InternalMessages type, ProductUserId clientUserID, SocketId socketId);
        protected abstract bool IsExpectedDataSocket(ProductUserId remoteUserId, SocketId socketId);
        protected abstract void OnReceiveData(byte[] data, ProductUserId clientUserID, int channel);
        protected abstract void OnConnectionFailed(ProductUserId remoteId);
    }
}
