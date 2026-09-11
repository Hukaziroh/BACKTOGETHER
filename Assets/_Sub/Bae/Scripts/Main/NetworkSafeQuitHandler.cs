using System.Collections;
using UnityEngine;
using Mirror;
using EpicTransport;

public class NetworkSafeQuitHandler : MonoBehaviour
{
    private static NetworkSafeQuitHandler instance;
    private bool isQuittingHandled = false;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            enabled = false;
            return;
        }

        instance = this;
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
        // Debug.Log("[NetworkSafeQuitHandler] Alt+F4 / [X] 버튼 종료 감지. 네트워크 안전 정리를 진행합니다.");

        EOSLobby eosLobby = FindAnyObjectByType<EOSLobby>();

        // 1. 전송 계층이 살아 있을 때 로비 파괴/퇴장부터 완료합니다.
        if (NetworkServer.active)
        {
            if (eosLobby != null && eosLobby.ConnectedToLobby)
            {
                eosLobby.DestroyLobby();
            }
        }
        else if (NetworkClient.active)
        {
            if (eosLobby != null && eosLobby.ConnectedToLobby)
            {
                eosLobby.LeaveLobby();
            }
        }

        if (eosLobby != null)
        {
            float leaveTimeout = 2f;
            while (eosLobby.IsLeavingLobby && leaveTimeout > 0f)
            {
                leaveTimeout -= Time.unscaledDeltaTime;
                yield return null;
            }
        }

        // 2. 로비 정리 뒤 Mirror/P2P를 종료합니다.
        if (NetworkManager.singleton != null)
        {
            if (NetworkServer.active)
            {
                NetworkManager.singleton.StopHost();
            }
            else if (NetworkClient.active)
            {
                NetworkManager.singleton.StopClient();
            }

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
        if (instance == this)
        {
            Application.wantsToQuit -= OnWantsToQuit;
            instance = null;
        }
    }
}
