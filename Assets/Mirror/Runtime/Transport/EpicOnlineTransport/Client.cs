using Epic.OnlineServices;
using Epic.OnlineServices.P2P;
using Mirror;
using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace EpicTransport
{
    public class Client : Common
    {

        public SocketId socketId;
        public ProductUserId serverId;
        public bool Connected { get; private set; }
        public bool Error { get; private set; }
        private event Action<byte[], int> OnReceivedData;
        private event Action OnConnected;
        public event Action OnDisconnected;
        private TimeSpan ConnectionTimeout;
        public bool isConnecting = false;
        public string hostAddress = "";
        private ProductUserId hostProductId = null;
        private TaskCompletionSource<bool> connectedComplete;
        private CancellationTokenSource cancelToken;

        private int connectionGenerationId = 0;
        private bool disconnecting;
        private bool disconnectNotified;
        private const int HandshakeRetryMilliseconds = 750;

        private Client(EosTransport transport) : base(transport)
        {
            ConnectionTimeout = TimeSpan.FromSeconds(Math.Max(1, transport.timeout));
        }

        public static Client CreateClient(EosTransport transport, string host)
        {
            Client c = new Client(transport);
            c.hostAddress = host;
            c.socketId = new SocketId() { SocketName = RandomString.Generate(20) };
            c.OnConnected += () => transport.OnClientConnected.Invoke();
            c.OnDisconnected += () => transport.OnClientDisconnected.Invoke();
            c.OnReceivedData += (data, channel) => transport.OnClientDataReceived.Invoke(new ArraySegment<byte>(data), channel);
            return c;
        }

        // ★ 핵심 방어막: 과거 연결 시도의 유령 타임아웃 튕겨내기!
        protected override void OnConnectFail(OnRemoteConnectionClosedInfo result)
        {
            if (ignoreAllMessages || disconnecting || IsDisposed || IsDeadSocket(result.SocketId)) return;

            if (this.socketId != null && result.SocketId != null && this.socketId.SocketName != result.SocketId.SocketName)
            {
                Debug.LogWarning($"[EpicTransport Client] 과거 유령 소켓({result.SocketId.SocketName}) 끊김 이벤트를 무시합니다. (현재: {this.socketId.SocketName})");
                return;
            }
            base.OnConnectFail(result);
        }

        public async void Connect(string host)
        {
            int currentAttemptId = ++connectionGenerationId;
            cancelToken?.Cancel();
            cancelToken?.Dispose();
            CancellationTokenSource attemptCancellation = new CancellationTokenSource();
            CancellationToken attemptToken = attemptCancellation.Token;
            cancelToken = attemptCancellation;
            disconnecting = false;
            disconnectNotified = false;
            Connected = false;
            Error = false;
            isConnecting = true;

            try
            {
                hostProductId = ProductUserId.FromString(host);
                serverId = hostProductId;
                connectedComplete = new TaskCompletionSource<bool>();

                DateTime deadline = DateTime.UtcNow + ConnectionTimeout;

                while (currentAttemptId == connectionGenerationId && !connectedComplete.Task.IsCompleted)
                {
                    SendInternal(hostProductId, socketId, InternalMessages.CONNECT);

                    TimeSpan remaining = deadline - DateTime.UtcNow;
                    if (remaining <= TimeSpan.Zero) break;

                    int retryDelay = Math.Min(
                        HandshakeRetryMilliseconds,
                        Math.Max(1, (int)remaining.TotalMilliseconds));

                    Task retryTask = Task.Delay(retryDelay, attemptToken);
                    Task finishedTask = await Task.WhenAny(connectedComplete.Task, retryTask);

                    if (finishedTask == connectedComplete.Task) break;
                    attemptToken.ThrowIfCancellationRequested();
                }

                if (currentAttemptId != connectionGenerationId) return;

                if (!connectedComplete.Task.IsCompleted)
                {
                    Debug.LogError($"Connection to {host} timed out.");
                    Error = true;
                    OnConnectionFailed(hostProductId);
                }
            }
            catch (OperationCanceledException)
            {
                // 정상적인 StopClient/Shutdown 경로입니다.
            }
            catch (FormatException ex)
            {
                if (currentAttemptId != connectionGenerationId) return;
                Debug.LogError($"Connection string was not in the right format: {ex.Message}");
                Error = true;
                OnConnectionFailed(hostProductId);
            }
            catch (Exception ex)
            {
                if (currentAttemptId != connectionGenerationId) return;
                Debug.LogException(ex);
                Error = true;
                OnConnectionFailed(hostProductId);
            }
            finally
            {
                if (ReferenceEquals(cancelToken, attemptCancellation))
                {
                    cancelToken = null;
                }

                attemptCancellation.Dispose();
            }
        }

        public void Disconnect()
        {
            if (disconnecting) return;
            disconnecting = true;
            connectionGenerationId++;
            isConnecting = false;

            ProductUserId remoteUserId = serverId ?? hostProductId;
            SocketId closingSocket = socketId;

            if (remoteUserId != null && closingSocket != null)
            {
                // 종료 패킷을 먼저 보낸 다음 EOS의 실제 P2P 연결을 닫아야 한다.
                SendInternal(remoteUserId, closingSocket, InternalMessages.DISCONNECT);
            }

            Dispose();
            cancelToken?.Cancel();
            cancelToken?.Dispose();
            cancelToken = null;

            if (remoteUserId != null && closingSocket != null)
            {
                CloseP2PSessionWithUser(remoteUserId, closingSocket);
            }

            Connected = false;
            serverId = null;
            hostProductId = null;

            // Mirror는 이 콜백을 받아야 NetworkClient.Shutdown으로 handler/prefab을 비운다.
            // EosTransport가 필드를 먼저 비우므로 콜백 중 재귀 ClientDisconnect도 안전하다.
            NotifyDisconnected();

            OnConnected = null;
            OnDisconnected = null;
            OnReceivedData = null;
        }

        protected override void OnReceiveData(byte[] data, ProductUserId clientUserId, int channel)
        {
            if (ignoreAllMessages) return;
            if (clientUserId != hostProductId) return;
            OnReceivedData?.Invoke(data, channel);
        }

        protected override bool IsExpectedDataSocket(ProductUserId remoteUserId, SocketId incomingSocket)
        {
            return !disconnecting &&
                   !IsDisposed &&
                   Connected &&
                   remoteUserId == hostProductId &&
                   IsSameSocket(socketId, incomingSocket);
        }

        protected override void OnNewConnection(OnIncomingConnectionRequestInfo result)
        {
            if (ignoreAllMessages || disconnecting || IsDisposed) return;
            if (IsDeadSocket(result.SocketId) || !IsSameSocket(socketId, result.SocketId)) return;

            if (hostProductId == result.RemoteUserId)
            {
                EOSSDKComponent.GetP2PInterface().AcceptConnection(new AcceptConnectionOptions() { LocalUserId = EOSSDKComponent.LocalUserProductId, RemoteUserId = result.RemoteUserId, SocketId = result.SocketId });
            }
        }

        protected override void OnReceiveInternalData(InternalMessages type, ProductUserId clientUserId, SocketId socketId)
        {
            if (ignoreAllMessages || disconnecting || IsDisposed) return;
            if (clientUserId != hostProductId || !IsSameSocket(this.socketId, socketId)) return;

            switch (type)
            {
                case InternalMessages.ACCEPT_CONNECT:
                    if (Connected) return;
                    Connected = true;
                    connectedComplete?.TrySetResult(true);
                    OnConnected?.Invoke();
                    break;
                case InternalMessages.DISCONNECT:
                    Connected = false;
                    NotifyDisconnected();
                    break;
            }
        }

        public void Send(byte[] data, int channelId) => Send(hostProductId, socketId, data, (byte)channelId);

        protected override void OnConnectionFailed(ProductUserId remoteId)
        {
            Connected = false;
            NotifyDisconnected();
        }

        public void EosNotInitialized()
        {
            Error = true;
            NotifyDisconnected();
        }

        private void NotifyDisconnected()
        {
            if (disconnectNotified) return;
            disconnectNotified = true;
            OnDisconnected?.Invoke();
        }
    }
}
