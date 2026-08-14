using System.Collections;
using UnityEngine;
using Mirror;
using EpicTransport;

public class NetworkSafeQuitHandler : MonoBehaviour
{
    private bool isQuittingHandled = false;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);

        // 유저가 Alt+F4, [X] 버튼, Application.Quit() 등을 누를 때 감지
        Application.wantsToQuit += OnWantsToQuit;
    }

    private bool OnWantsToQuit()
    {
        // 이미 안전하게 청소 후 종료 중이라면 그대로 종료 허용
        if (isQuittingHandled) return true;

        // 종료 프로세스 시작 (유니티 즉시 종료를 일단 차단)
        StartCoroutine(SafeQuitRoutine());
        return false; // false를 반환하면 유니티가 꺼지는 것을 잠시 멈춥니다.
    }

    private IEnumerator SafeQuitRoutine()
    {
        Debug.Log("[NetworkSafeQuitHandler] Alt+F4 / [X] 버튼 종료 감지. 네트워크 안전 정리를 진행합니다.");

        // 1. 내가 방장(Host)인 경우 로비 파괴 및 서버 정지
        if (NetworkServer.active)
        {
            EOSLobby eosLobby = FindAnyObjectByType<EOSLobby>();
            if (eosLobby != null && eosLobby.ConnectedToLobby)
            {
                eosLobby.DestroyLobby();
            }

            if (NetworkManager.singleton != null)
            {
                NetworkManager.singleton.StopHost();
            }
        }
        // 2. 내가 클라이언트인 경우 로비 퇴장 및 클라이언트 정지
        else if (NetworkClient.active)
        {
            EOSLobby eosLobby = FindAnyObjectByType<EOSLobby>();
            if (eosLobby != null && eosLobby.ConnectedToLobby)
            {
                eosLobby.LeaveLobby();
            }

            if (NetworkManager.singleton != null)
            {
                NetworkManager.singleton.StopClient();
            }
        }

        // 3. 에픽 P2P 트랜스포트 세션 강제 종료
        if (NetworkManager.singleton != null)
        {
            EosTransport transport = NetworkManager.singleton.GetComponent<EosTransport>();
            if (transport != null)
            {
                transport.Shutdown();
            }
        }

        // 4. 에픽 백엔드로 패킷이 발송될 수 있도록 0.3초 대기
        yield return new WaitForSecondsRealtime(0.3f);

        // 5. 정리 완료 후 진짜로 유니티 종료 허용
        isQuittingHandled = true;
        Application.Quit();
    }

    private void OnDestroy()
    {
        Application.wantsToQuit -= OnWantsToQuit;
    }
}