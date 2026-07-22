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
        Debug.Log("[GameQuitHandler] 게임 종료 프로세스 시작...");

        if (NetworkManager.singleton != null)
        {
            if (NetworkServer.active) NetworkManager.singleton.StopHost();
            else if (NetworkClient.active || NetworkClient.isConnected) NetworkManager.singleton.StopClient();
        }
        if (EpicTransport.EOSSDKComponent.instance != null)
        {
            EpicTransport.EOSSDKComponent.instance.SafeReleaseEOS();
        }
        yield return new WaitForSecondsRealtime(0.5f);

        Debug.Log("[GameQuitHandler] EOS 및 네트워크 정리 완료, 에디터 종료");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}