using UnityEngine;

// 스팀 빌드일 때만 Steamworks 네임스페이스를 가져옵니다.
#if STEAM_BUILD
using Steamworks;
#endif

// 스토브 빌드일 때만 스토브 관련 구조체를 사용합니다.
#if STOVE_BUILD
using Stove.PCSDK;
#endif

/// <summary>
/// 스팀과 스토브 플랫폼의 API를 하나의 창구로 통합해서 관리하는 스크립트입니다.
/// 게임 내 다른 스크립트들은 스팀/스토브를 신경 쓸 필요 없이 오직 이 매니저만 호출하면 됩니다.
/// </summary>
public class PlatformManager : Singleton<PlatformManager>
{
    protected override void Awake()
    {
        base.Awake();
        if (Instance == this)
        {
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }
    }

    private void Start()
    {
        InitializePlatform();
    }

    /// <summary>
    /// 현재 빌드된 플랫폼 환경이 무엇인지 확인하고 초기화 상태를 체크합니다.
    /// </summary>
    private void InitializePlatform()
    {
#if STOVE_BUILD
        Debug.Log("🔥 [PlatformManager] STOVE 환경으로 빌드되었습니다. StovePCSDK3Manager가 백그라운드에서 작동합니다.");
        
#elif STEAM_BUILD
        Debug.Log("💨 [PlatformManager] STEAM 환경으로 빌드되었습니다. SteamManager가 백그라운드에서 작동합니다.");
        if (SteamManager.Initialized)
        {
            Debug.Log("스팀 API 초기화 성공!");
        }
#else
        Debug.Log("💻 [PlatformManager] 에디터 또는 플랫폼 독립 테스트 환경입니다.");
#endif
    }

    /// <summary>
    /// 현재 접속한 유저의 닉네임을 가져옵니다.
    /// 사용 예: string myName = PlatformManager.Instance.GetPlayerName();
    /// </summary>
    public string GetPlayerName()
    {
#if STOVE_BUILD
        if (StovePCSDK3Manager.InstanceExists && StovePCSDK3Manager.Instance.isInitialized)
        {
            var userResult = StovePCSDK3Manager.Instance.GetUser();
            if (userResult.Item1) 
            {
                return userResult.Item2.nickname; // 스토브 닉네임 반환
            }
        }
        return "Stove_User"; // 실패 시 임시 이름

#elif STEAM_BUILD
        if (SteamManager.Initialized)
        {
            return SteamFriends.GetPersonaName(); // 스팀 닉네임 반환
        }
        return "Steam_User";

#else
        return "Local_Test_Player";
#endif
    }

    /// <summary>
    /// 도전과제(업적)를 달성 처리합니다.
    /// 사용 예: PlatformManager.Instance.UnlockAchievement("CLEAR_CHAPTER_1");
    /// </summary>
    /// <summary>
    /// 도전과제(업적)를 달성 처리합니다.
    /// </summary>
    public void UnlockAchievement(string achievementId)
    {
#if STOVE_BUILD
    if (StovePCSDK3Manager.InstanceExists && StovePCSDK3Manager.Instance.isInitialized)
    {
        // 👈 방금 만든 스토브 전용 함수 호출!
        StovePCSDK3Manager.Instance.UnlockAchievement(achievementId);
        Debug.Log($"[STOVE] 업적/스탯 달성 요청 전송: {achievementId}");
    }

#elif STEAM_BUILD
    if (SteamManager.Initialized)
    {
        Steamworks.SteamUserStats.SetAchievement(achievementId);
        Steamworks.SteamUserStats.StoreStats();
        Debug.Log($"[STEAM] 업적 달성: {achievementId}");
    }
#endif
    }
}