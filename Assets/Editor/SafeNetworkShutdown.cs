#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Mirror;

[InitializeOnLoad]
public class SafeNetworkShutdown
{
    static SafeNetworkShutdown()
    {
        // 컴파일 시 중복 등록 방지를 위해 먼저 제거 후 등록
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingPlayMode)
        {
            if (NetworkManager.singleton != null)
            {
                Debug.Log("[Editor 셧다운] 네트워크 안전 종료 프로세스 시작");

                // 1. 강제 파괴 전, Mirror 네트워크 세션을 먼저 안전하게 정지 시도
                if (NetworkServer.active || NetworkClient.active)
                {
                    NetworkManager.singleton.StopHost();
                }

                // 2. 만약 오브젝트 참조가 남아있다면 안전하게 파괴
                if (NetworkManager.singleton != null && NetworkManager.singleton.gameObject != null)
                {
                    Object.DestroyImmediate(NetworkManager.singleton.gameObject);
                }
            }
        }
    }
}
#endif