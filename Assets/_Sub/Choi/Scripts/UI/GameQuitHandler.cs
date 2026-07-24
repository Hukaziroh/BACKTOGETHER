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
        Debug.Log("[GameQuitHandler] 게임 안전 종료 프로세스 시작...");

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
        }
        yield return new WaitForSecondsRealtime(0.3f);

        Debug.Log("[GameQuitHandler] 네트워크 정리 완료, 안전하게 종료합니다.");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}