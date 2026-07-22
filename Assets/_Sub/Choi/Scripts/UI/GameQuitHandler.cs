using UnityEngine;

public class GameQuitHandler : MonoBehaviour
{
    private bool isQuitting = false;

    // UI의 EXIT (게임 종료) 버튼 OnClick에 연결하세요.
    public void QuitGame()
    {
        if (isQuitting) return;
        isQuitting = true;

        HostDisconnectHandler disconnectHandler = FindAnyObjectByType<HostDisconnectHandler>();
        if (disconnectHandler != null)
        {
            disconnectHandler.SetIntentionalExit();
        }

        Debug.Log("[GameQuitHandler] 게임 종료 요청 - SafeNetworkShutdown이 안전 정리를 담당합니다.");

#if UNITY_EDITOR
        // 플레이 모드를 끄는 순간 SafeNetworkShutdown이 ExitingPlayMode를 감지하여
        // NetworkManager를 즉시 파괴(DestroyImmediate)하므로 멈춤 현상이 해결됩니다.
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}