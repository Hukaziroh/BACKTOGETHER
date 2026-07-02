#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Mirror;

[InitializeOnLoad]
public class SafeNetworkShutdown
{
    static SafeNetworkShutdown()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingPlayMode)
        {
            if (NetworkManager.singleton != null)
            {
                if (NetworkServer.active)
                {
                    Debug.Log("[Editor 셧다운] 호스트 안전 종료 중...");
                    NetworkManager.singleton.StopHost();
                }
                else if (NetworkClient.isConnected || NetworkClient.active)
                {
                    Debug.Log("[Editor 셧다운] 클라이언트 안전 종료 중...");
                    NetworkManager.singleton.StopClient();
                }
            }
            if (Transport.active != null)
            {
                Debug.Log("[Editor 셧다운] 트랜스포트 강제 초기화 중...");
                Transport.active.ClientDisconnect();
                Transport.active.Shutdown();
            }
        }
    }
}
#endif