using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System;
using System.Linq;
using Mirror;
using EpicTransport;

#if STEAM_BUILD
using Steamworks;
#endif

#if STOVE_BUILD
// STOVE SDK 네임스페이스 (프로젝트 설정에 맞게 수정될 수 있음)
using Stove.PCSDK;
using static Stove.PCSDK.GameSupport;
#endif

public class PlatformInviteManager : MonoBehaviour
{
    public static PlatformInviteManager Instance { get; private set; }

    [Header("Settings")]
    [Tooltip("초대 수락 시 진입해야 할 씬 이름")]
    [SerializeField] private string targetSceneName = "Main";

    // 지연 접속용 임시 저장소 (초대를 수락했지만 아직 Main 씬이 아닐 때)
    private string pendingInviteCode = "";

#if STEAM_BUILD
    protected Callback<GameLobbyJoinRequested_t> m_GameLobbyJoinRequested;
    protected CallResult<LobbyCreated_t> m_LobbyCreated;
    protected CallResult<LobbyEnter_t> m_LobbyEnter;

    private CSteamID currentSteamLobbyId = CSteamID.Nil;
    private string currentHostingCode = "";
#endif

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;

        // 1. 커맨드라인 인수(런처 외부 실행 등) 파싱
        ParseCommandLineArguments();

#if STEAM_BUILD
        // SteamManager.Initialized 체크 없이 콜백 객체를 미리 생성해둡니다. (Start 호출 순서 문제 방지)
        m_GameLobbyJoinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnGameLobbyJoinRequested);
        m_LobbyCreated = CallResult<LobbyCreated_t>.Create(OnLobbyCreated);
        m_LobbyEnter = CallResult<LobbyEnter_t>.Create(OnLobbyEnter);
#endif
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

#if STEAM_BUILD
        if (m_GameLobbyJoinRequested != null) m_GameLobbyJoinRequested.Dispose();
        if (m_LobbyCreated != null) m_LobbyCreated.Dispose();
        if (m_LobbyEnter != null) m_LobbyEnter.Dispose();

        if (currentSteamLobbyId.IsValid())
        {
            SteamMatchmaking.LeaveLobby(currentSteamLobbyId);
            currentSteamLobbyId = CSteamID.Nil;
        }
#endif
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == targetSceneName)
        {
            CheckPendingInvite();
        }
    }

    /// <summary>
    /// 방장이 PrivateLobbyManager에서 방을 성공적으로 만들었을 때 호출합니다.
    /// 스팀 로비를 백그라운드에 생성하고 코드를 연동합니다.
    /// </summary>
    public void SetLobbyDataForInvite(string shortCode)
    {
        Debug.Log($"[PlatformInviteManager] 외부 플랫폼 로비 세팅 요청됨 (ShortCode: {shortCode})");

#if STEAM_BUILD
        if (!SteamManager.Initialized)
        {
            Debug.LogError("[PlatformInviteManager] SteamManager가 초기화되지 않아서 스팀 로비를 만들 수 없습니다!");
            return;
        }

        // 기존에 파둔 스팀 로비가 있다면 확실하게 퇴장해서 꼬임을 방지합니다.
        if (currentSteamLobbyId.IsValid())
        {
            SteamMatchmaking.LeaveLobby(currentSteamLobbyId);
            currentSteamLobbyId = CSteamID.Nil;
        }

        currentHostingCode = shortCode;

        // 친구 초대용으로 4인용 비공개(FriendsOnly) 스팀 로비 생성
        SteamAPICall_t handle = SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypeFriendsOnly, 4);
        m_LobbyCreated.Set(handle);

        Debug.Log($"[PlatformInviteManager] 스팀 로비 생성 진행 중... (ShortCode: {shortCode})");
#endif

#if STOVE_BUILD
        // STOVE_BUILD: 스토브는 런처의 RichPresence 또는 멀티플레이어 세션 API를 통해 세션 ID를 공유합니다.
        // Stove.PCSDK v3 명세에 따라 세션 ID를 설정합니다. (아래는 일반적인 형태이며, SDK 버전에 맞춰 수정)
        // GameSupport_SetMultiplayerSessionId(shortCode, (result) => { ... });
        Debug.Log($"[PlatformInviteManager] 스토브 세션 ID 등록 요청 (ShortCode: {shortCode})");
#endif
    }

#if STEAM_BUILD
    private void OnLobbyCreated(LobbyCreated_t pCallback, bool bIOFailure)
    {
        if (bIOFailure || pCallback.m_eResult != EResult.k_EResultOK)
        {
            Debug.LogError("[PlatformInviteManager] 스팀 로비 생성 실패");
            return;
        }

        currentSteamLobbyId = new CSteamID(pCallback.m_ulSteamIDLobby);
        
        // 생성된 스팀 로비에 EOS 방 코드를 메타데이터로 기록
        SteamMatchmaking.SetLobbyData(currentSteamLobbyId, "EOS_SHORTCODE", currentHostingCode);
        
        Debug.Log($"[PlatformInviteManager] 스팀 로비 생성 완료 및 EOS_SHORTCODE 등록 성공 ({currentHostingCode})");
    }

    private void OnGameLobbyJoinRequested(GameLobbyJoinRequested_t pCallback)
    {
        Debug.Log("[PlatformInviteManager] 친구 초대를 수락했습니다. 스팀 로비에 접속을 시도합니다.");

        // 스팀 로비 접속 시도
        SteamAPICall_t handle = SteamMatchmaking.JoinLobby(pCallback.m_steamIDLobby);
        m_LobbyEnter.Set(handle);
    }

    private void OnLobbyEnter(LobbyEnter_t pCallback, bool bIOFailure)
    {
        if (bIOFailure || pCallback.m_EChatRoomEnterResponse != (uint)EChatRoomEnterResponse.k_EChatRoomEnterResponseSuccess)
        {
            Debug.LogError("[PlatformInviteManager] 초대받은 스팀 로비 접속에 실패했습니다.");
            return;
        }

        CSteamID steamLobbyId = new CSteamID(pCallback.m_ulSteamIDLobby);
        
        // 스팀 로비에서 EOS_SHORTCODE 데이터를 읽어옴
        string shortCode = SteamMatchmaking.GetLobbyData(steamLobbyId, "EOS_SHORTCODE");

        if (!string.IsNullOrEmpty(shortCode))
        {
            Debug.Log($"[PlatformInviteManager] 스팀 로비에서 EOS_SHORTCODE ({shortCode})를 성공적으로 가져왔습니다!");
            
            // 즉시 스팀 로비에서는 퇴장 (메신저 역할 끝)
            SteamMatchmaking.LeaveLobby(steamLobbyId);

            // 실제 게임 접속 처리 시작
            HandleInviteCode(shortCode);
        }
        else
        {
            Debug.LogError("[PlatformInviteManager] 초대받은 스팀 로비에 EOS_SHORTCODE 데이터가 없습니다.");
        }
    }
#endif

    /// <summary>
    /// 게임이 꺼져있을 때 스토브/스팀 런처에서 초대를 수락하여 켜진 경우, 커맨드라인 인수를 파싱합니다.
    /// </summary>
    private void ParseCommandLineArguments()
    {
        string[] args = Environment.GetCommandLineArgs();
        if (args == null || args.Length == 0) return;

        // 예시 1: 스팀/에픽 게임즈 "+connect 123456" 형태
        // 예시 2: 스토브 "--join-session=123456" 형태
        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i].ToLower();
            
            if (arg == "+connect" || arg == "-connect")
            {
                if (i + 1 < args.Length)
                {
                    Debug.Log($"[PlatformInviteManager] 커맨드라인에서 방 코드 발견: {args[i + 1]}");
                    HandleInviteCode(args[i + 1]);
                    return;
                }
            }
            else if (arg.StartsWith("--join-session=") || arg.StartsWith("-join-session="))
            {
                string[] parts = args[i].Split('=');
                if (parts.Length > 1)
                {
                    Debug.Log($"[PlatformInviteManager] 스토브 런처 커맨드라인에서 방 코드 발견: {parts[1]}");
                    HandleInviteCode(parts[1]);
                    return;
                }
            }
        }
    }

    private void HandleInviteCode(string code)
    {
        pendingInviteCode = code;
        StartCoroutine(LeaveCurrentSessionAndJoinRoutine());
    }

    private IEnumerator LeaveCurrentSessionAndJoinRoutine()
    {
        // 1. 이미 접속 중인 게임이나 로비가 있다면 완전히 연결을 끊고 나갑니다.
        if (NetworkManager.singleton != null && (NetworkServer.active || NetworkClient.active))
        {
            Debug.Log("[PlatformInviteManager] 기존 네트워크 세션 감지됨. 연결을 끊습니다.");

            HostDisconnectHandler disconnectHandler = FindAnyObjectByType<HostDisconnectHandler>();
            if (disconnectHandler != null)
            {
                disconnectHandler.SetIntentionalExit();
            }

            EOSLobby eosLobby = FindAnyObjectByType<EOSLobby>();
            if (eosLobby != null && eosLobby.ConnectedToLobby)
            {
                if (NetworkServer.active) eosLobby.DestroyLobby();
                else eosLobby.LeaveLobby();

                float timeout = 3f;
                while (eosLobby.IsLeavingLobby && timeout > 0f)
                {
                    timeout -= Time.unscaledDeltaTime;
                    yield return null;
                }
            }

            if (NetworkServer.active) NetworkManager.singleton.StopHost();
            else if (NetworkClient.active) NetworkManager.singleton.StopClient();

            // 연결이 완전히 끊기고 초기화될 때까지 대기
            yield return new WaitForSecondsRealtime(0.5f);
        }

        // 2. 메인 씬이 아니면 메인 씬으로 이동 후 초대 처리
        if (SceneManager.GetActiveScene().name == targetSceneName)
        {
            CheckPendingInvite();
        }
        else
        {
            Debug.Log($"[PlatformInviteManager] 현재 씬이 {targetSceneName}이 아닙니다. 메인으로 이동 후 접속합니다.");
            SceneManager.LoadScene(targetSceneName);
        }
    }

    private void CheckPendingInvite()
    {
        if (string.IsNullOrEmpty(pendingInviteCode)) return;

        string codeToJoin = pendingInviteCode;
        pendingInviteCode = ""; // 소비

        ClientJoinUI joinUI = FindAnyObjectByType<ClientJoinUI>();
        if (joinUI != null)
        {
            Debug.Log($"[PlatformInviteManager] {codeToJoin} 코드로 자동 접속을 시작합니다.");
            joinUI.AutoConnectByCode(codeToJoin);
        }
        else
        {
            Debug.LogError("[PlatformInviteManager] ClientJoinUI를 찾을 수 없어 자동 접속을 실행하지 못했습니다.");
        }
    }
}
