#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Mirror;

[InitializeOnLoad]
public class SafeNetworkShutdown
{
    static SafeNetworkShutdown()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        // 유니티 에디터의 '재생(Play)' 버튼을 눌러서 강제로 끌 때 작동
        if (state == PlayModeStateChange.ExitingPlayMode)
        {
            if (NetworkManager.singleton != null)
            {
                // 강제로 부수지(DestroyImmediate) 마세요! EOS 스레드가 굳습니다.
                // 그저 호스트/클라이언트 연결만 끊어주면 NetworkManager가 알아서 정리합니다.
                if (NetworkServer.active || NetworkClient.active)
                {
                    Debug.Log("[Editor 셧다운] 네트워크 세션 안전 종료...");
                    NetworkManager.singleton.StopHost();
                }
            }
        }
    }
}
#endif