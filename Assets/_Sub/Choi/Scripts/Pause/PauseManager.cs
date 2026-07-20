using UnityEngine;
using UnityEngine.SceneManagement;
using Mirror;
using System.Collections;

public class PauseManager : MonoBehaviour
{
    public static PauseManager instance;

    [Header("UI 패널 할당")]
    public GameObject pausePanel;

    [Header("설정")]
    public string mainMenuSceneName = "Main";
    public bool isPaused = false; // UIManager가 상태를 읽을 수 있게 public

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // --- 퍼즈/재개 로직 ---
    public void PauseGame()
    {
        if (pausePanel != null) pausePanel.SetActive(true);
        isPaused = true;

        // ★ [추가] 퍼즈창이 켜질 때 포커스를 퍼즈창 내부로 격리 (뒷배경 UI 차단)
        if (GlobalSceneInputManager.Instance != null && pausePanel != null)
        {
            GlobalSceneInputManager.Instance.SetFocusScope(pausePanel);
        }
    }

    public void ResumeGame()
    {
        if (pausePanel != null) pausePanel.SetActive(false);
        isPaused = false;

        // ★ [추가] 퍼즈창이 닫힐 때 포커스 격리를 해제하고 배경 UI들을 원래대로 복구
        if (GlobalSceneInputManager.Instance != null)
        {
            GlobalSceneInputManager.Instance.ClearFocusScope();
        }
    }

    // --- 옵션창 전환 (자신은 숨기고 옵션 매니저를 호출) ---
    public void OpenOptions()
    {
        if (pausePanel != null) pausePanel.SetActive(false);
        OptionsManager.instance.Open();

        // ★ [추가] 옵션창이 열릴 때 포커스 범위를 옵션 매니저 UI 내부로 전환
        if (GlobalSceneInputManager.Instance != null && OptionsManager.instance != null)
        {
            GlobalSceneInputManager.Instance.SetFocusScope(OptionsManager.instance.gameObject);
        }
    }

    // 옵션에서 뒤로가기 눌렀을 때 호출
    public void CloseOptions()
    {
        OptionsManager.instance.Close();
        if (pausePanel != null) pausePanel.SetActive(true);

        // ★ [추가] 옵션에서 다시 퍼즈창으로 돌아올 때 퍼즈창 내부로 포커스 재격리
        if (GlobalSceneInputManager.Instance != null && pausePanel != null)
        {
            GlobalSceneInputManager.Instance.SetFocusScope(pausePanel);
        }
    }

    // --- 시스템 로직 (그대로 유지) ---
    public void ReturnToMainMenu()
    {
        ResumeGame();

        // 1. 내가 스스로 나가는 것이므로 튕김 UI 방지 (기존 코드 유지)
        HostDisconnectHandler disconnectHandler = FindAnyObjectByType<HostDisconnectHandler>();
        if (disconnectHandler != null)
        {
            disconnectHandler.SetIntentionalExit();
        }

        // 2. 🌟 즉시 씬을 바꾸지 않고, 안전하게 대기한 뒤 나가는 코루틴 실행
        StartCoroutine(LeaveGameGracefullyRoutine());
    }

    private IEnumerator LeaveGameGracefullyRoutine()
    {
        Debug.Log("[퍼즈 시스템] 안전한 게임 퇴장 절차를 시작합니다...");

        if (NetworkManager.singleton != null)
        {
            EOSLobby eosLobby = NetworkManager.singleton.GetComponent<EOSLobby>();
            if (eosLobby != null && eosLobby.ConnectedToLobby)
            {
                Debug.Log("[퍼즈 시스템] 에픽 온라인 서비스 로비를 나가는 중...");
                eosLobby.LeaveLobby();
                yield return new WaitForSecondsRealtime(0.3f);
            }
        }

        if (NetworkServer.active)
        {
            Debug.Log("[퍼즈 시스템] 호스트(방장) 서버를 종료합니다.");
            NetworkManager.singleton.StopHost();
        }
        else if (NetworkClient.isConnected)
        {
            Debug.Log("[퍼즈 시스템] 클라이언트 접속을 종료합니다.");
            NetworkManager.singleton.StopClient();
        }

        Debug.Log("[퍼즈 시스템] Mirror 엔진에 의해 메인 화면으로 자동 전환됩니다.");
        yield return null;
    }
}