using UnityEngine;
using Mirror;
using System.Collections;

public class GameQuitHandler : MonoBehaviour
{
    private bool isQuitting = false;

    public void QuitGame()
    {
        if (isQuitting) return;
        isQuitting = true;

        HostDisconnectHandler disconnectHandler = FindAnyObjectByType<HostDisconnectHandler>();
        if (disconnectHandler != null)
        {
            disconnectHandler.SetIntentionalExit();
        }

        StartCoroutine(GracefulQuitRoutine());
    }

    private IEnumerator GracefulQuitRoutine()
    {
        // Debug.Log("[GameQuitHandler] 게임 안전 종료 프로세스 시작...");

        // =========================================================
        // 1. 네트워크 관리자 종료 및 명시적 파괴 (크래시 방지 핵심)
        // =========================================================
        if (NetworkManager.singleton != null)
        {
            if (NetworkServer.active)
            {
                NetworkManager.singleton.StopHost();
            }
            else if (NetworkClient.active || NetworkClient.isConnected)
            {
                NetworkManager.singleton.StopClient();
            }

            // 🚨 유니티가 랜덤하게 부수기 전에 우리가 통째로 삭제해버립니다.
            // EOSTransport가 OnDestroy 이벤트를 정상적으로 타고 내려가며 메모리를 안전하게 비웁니다.
            Destroy(NetworkManager.singleton.gameObject);
        }

        // =========================================================
        // 2. EOS Lobby 객체 파괴
        // =========================================================
        EOSLobby eosLobby = FindAnyObjectByType<EOSLobby>();
        if (eosLobby != null)
        {
            Destroy(eosLobby.gameObject);
        }

        // =========================================================
        // 3. 네이티브 메모리(C 플러그인) 해제 대기 (0.3 -> 0.5초로 여유 확보)
        // =========================================================
        yield return new WaitForSecondsRealtime(0.5f);

        // Debug.Log("[GameQuitHandler] 네트워크 및 EOS 정리 완료, 안전하게 종료합니다.");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}