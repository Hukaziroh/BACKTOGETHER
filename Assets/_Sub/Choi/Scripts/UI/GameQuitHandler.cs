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

        // 즉시 끄지 않고, 안전 종료 코루틴 실행
        StartCoroutine(GracefulQuitRoutine());
    }

    private IEnumerator GracefulQuitRoutine()
    {
        Debug.Log("[GameQuitHandler] 네트워크 정지 요청...");

        if (NetworkManager.singleton != null)
        {
            if (NetworkServer.active || NetworkClient.active)
            {
                NetworkManager.singleton.StopHost();
            }
        }

        // ★ 핵심: EOS 백그라운드 스레드가 소켓을 닫고 정리할 수 있는 '현실 시간' 보장
        // 1프레임으로는 가끔 부족하기 때문에 0.3초 대기합니다.
        yield return new WaitForSecondsRealtime(0.3f);

        Debug.Log("[GameQuitHandler] 네트워크 정리 완료, 에디터 종료");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}