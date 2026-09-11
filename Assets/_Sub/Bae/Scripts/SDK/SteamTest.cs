using UnityEngine;
// STEAM_BUILD일 때만 Steamworks 라이브러리를 쓰도록 처리
#if STEAM_BUILD
using Steamworks;
#endif

public class SteamTest : MonoBehaviour
{
    void Start()
    {
        // 🚨 핵심 방어 로직
#if !STEAM_BUILD
        Destroy(gameObject);
        return;
#endif

        // 아래는 STEAM_BUILD 일 때만 컴파일되는 로직
#if STEAM_BUILD
        if (SteamManager.Initialized)
        {
            string myName = SteamFriends.GetPersonaName();
            // Debug.Log($"✅ 스팀 연동 성공! 환영합니다, {myName}님!");
        }
        else
        {
            Debug.LogError("❌ 스팀 연동 실패: 스팀 클라이언트가 켜져있는지 확인하세요.");
        }
#endif
    }
}