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
        // 유니티 플레이 모드가 꺼지기 '직전(ExitingPlayMode)'에 단 한 번 실행됩니다.
        if (state == PlayModeStateChange.ExitingPlayMode)
        {
            if (NetworkManager.singleton != null && NetworkManager.singleton.gameObject != null)
            {
                Debug.Log("[Editor 셧다운] 에디터 크래시 방지: 네트워크 매니저 우선 파괴");

                // 💡 핵심: 어설픈 명령어들을 모두 빼고, 유니티 에디터가 메모리를 무작위로 부수기 전에
                // 네트워크 매니저 오브젝트를 '제일 먼저, 그리고 온전하게' 소멸시켜버립니다.
                // 이 한 줄이 Mirror와 에픽 트랜스포트의 자체 안전 종료 로직을 정상적으로 발동시킵니다.
                Object.DestroyImmediate(NetworkManager.singleton.gameObject);
            }
        }
    }
}
#endif