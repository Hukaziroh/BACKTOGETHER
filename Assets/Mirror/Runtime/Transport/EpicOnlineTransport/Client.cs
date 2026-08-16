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
        private TaskCompletionSource<Task> connectedComplete;
        private CancellationTokenSource cancelToken;

        private int connectionGenerationId = 0;

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
            if (ignoreAllMessages) return;

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
            cancelToken = new CancellationTokenSource();

            try
            {
                hostProductId = ProductUserId.FromString(host);
                serverId = hostProductId;
                connectedComplete = new TaskCompletionSource<Task>();
                OnConnected += SetConnectedComplete;
                SendInternal(hostProductId, socketId, InternalMessages.CONNECT);

                Task connectedCompleteTask = connectedComplete.Task;
                if (await Task.WhenAny(connectedCompleteTask, Task.Delay(ConnectionTimeout)) != connectedCompleteTask)
                {
                    if (currentAttemptId != connectionGenerationId) return; // 유령 비동기 차단
                    Debug.LogError($"Connection to {host} timed out.");
                    OnConnected -= SetConnectedComplete;
                    OnConnectionFailed(hostProductId);
                }
                OnConnected -= SetConnectedComplete;
            }
            catch (FormatException)
            {
                if (currentAttemptId != connectionGenerationId) return;
                Error = true; OnConnectionFailed(hostProductId);
            }
            catch (Exception ex)
            {
                if (currentAttemptId != connectionGenerationId) return;
                Error = true; OnConnectionFailed(hostProductId);
            }
            finally
            {
                if (Error && currentAttemptId == connectionGenerationId) OnConnectionFailed(null);
            }
        }

        public void Disconnect()
        {
            connectionGenerationId++;
            if (serverId != null) { CloseP2PSessionWithUser(serverId, socketId); serverId = null; }
            else return;

            SendInternal(hostProductId, socketId, InternalMessages.DISCONNECT);
            Dispose();
            cancelToken?.Cancel();
            WaitForClose(hostProductId, socketId);

            OnConnected = null;
            OnDisconnected = null;
            OnReceivedData = null;
        }

        private void SetConnectedComplete() => connectedComplete.SetResult(connectedComplete.Task);

        protected override void OnReceiveData(byte[] data, ProductUserId clientUserId, int channel)
        {
            if (ignoreAllMessages) return;
            if (clientUserId != hostProductId) return;
            OnReceivedData?.Invoke(data, channel);
        }

        protected override void OnNewConnection(OnIncomingConnectionRequestInfo result)
        {
            if (ignoreAllMessages) return;
            if (deadSockets.Contains(result.SocketId.SocketName)) return;

            if (hostProductId == result.RemoteUserId)
            {
                EOSSDKComponent.GetP2PInterface().AcceptConnection(new AcceptConnectionOptions() { LocalUserId = EOSSDKComponent.LocalUserProductId, RemoteUserId = result.RemoteUserId, SocketId = result.SocketId });
            }
        }

        protected override void OnReceiveInternalData(InternalMessages type, ProductUserId clientUserId, SocketId socketId)
        {
            if (ignoreAllMessages) return;
            switch (type)
            {
                case InternalMessages.ACCEPT_CONNECT:
                    Connected = true; OnConnected?.Invoke(); break;
                case InternalMessages.DISCONNECT:
                    Connected = false; OnDisconnected?.Invoke(); break;
            }
        }

        public void Send(byte[] data, int channelId) => Send(hostProductId, socketId, data, (byte)channelId);
        protected override void OnConnectionFailed(ProductUserId remoteId) => OnDisconnected?.Invoke();
        public void EosNotInitialized() => OnDisconnected?.Invoke();
    }
}