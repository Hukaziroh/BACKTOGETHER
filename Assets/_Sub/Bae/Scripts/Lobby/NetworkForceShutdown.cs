using UnityEngine;
using Mirror;
using EpicTransport; // 에픽 사용 중이므로 필수

public class NetworkForceShutdown : MonoBehaviour
{
    void OnApplicationQuit()
    {
        // 1. 클라이언트 연결 강제 종료
        if (NetworkClient.isConnected)
        {
            NetworkClient.Disconnect();
            Debug.Log("[네트워크] 클라이언트 연결 강제 종료");
        }

        // 2. 서버 연결 강제 종료
        if (NetworkServer.active)
        {
            NetworkServer.Shutdown();
            Debug.Log("[네트워크] 서버 연결 강제 종료");
        }

        // 3. 에픽 트랜스포트 세션 닫기
        // EpicTransport는 내부적으로 세션을 붙잡고 있을 수 있으므로 Transport를 가져와 셧다운
        if (NetworkManager.singleton != null && NetworkManager.singleton.transport != null)
        {
            NetworkManager.singleton.transport.Shutdown();
            Debug.Log("[네트워크] 트랜스포트 셧다운");
        }
    }
}