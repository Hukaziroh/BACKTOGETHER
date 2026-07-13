using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("기능 매니저 연결")]
    [SerializeField] private PauseManager pauseManager;
    [SerializeField] private OptionsManager optionsManager;

    [Header("UI 연결")]
    [SerializeField] private GameObject progressTrackerUI;

    [Header("관전 연결")]
    [SerializeField] private SpectatorSystem spectatorSystem;

    [Header("씬 이름 설정")]
    // 인펙터에서 자유롭게 수정할 수 있도록 변수로 분리했습니다.
    [SerializeField] private string mainSceneName = "Main";
    [SerializeField] private string lobbySceneName = "Lobby";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    private void Update()
    {
        // 1. ESC 입력 처리
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            HandleEscapeInput();
        }

        // 2. 관전 키 입력 처리
        if (spectatorSystem != null && Keyboard.current != null)
        {
            if (Keyboard.current.qKey.wasPressedThisFrame) spectatorSystem.StopSpectating();

            if (Keyboard.current.digit1Key.wasPressedThisFrame) spectatorSystem.SelectTarget(0);
            if (Keyboard.current.digit2Key.wasPressedThisFrame) spectatorSystem.SelectTarget(1);
            if (Keyboard.current.digit3Key.wasPressedThisFrame) spectatorSystem.SelectTarget(2);
            if (Keyboard.current.digit4Key.wasPressedThisFrame) spectatorSystem.SelectTarget(3);

            if (Keyboard.current.tabKey.wasPressedThisFrame) spectatorSystem.CycleTarget();
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // ★ [핵심] 메인 씬도 아니고 "로비 씬도 아닐 때" (즉, 인게임 챕터 씬일 때만) 프로그레스 바를 활성화합니다.
        bool isChapter = (scene.name != mainSceneName && scene.name != lobbySceneName);
        progressTrackerUI?.SetActive(isChapter);

        // 관전 카메라 자동 연결 로직
        GameObject mainCam = GameObject.FindGameObjectWithTag("MainCamera");
        if (mainCam != null)
        {
            if (mainCam.GetComponent<SpectatorCamera>() == null)
            {
                mainCam.AddComponent<SpectatorCamera>();
                Debug.Log("[UIManager] 메인 카메라에 관전 기능을 자동으로 부착했습니다.");
            }
        }
    }

    private void HandleEscapeInput()
    {
        string currentScene = SceneManager.GetActiveScene().name;

        // ★ [핵심] 오직 메인 씬("Main")에서만 ESC 입력을 무시합니다. (로비 씬은 무사히 통과하여 퍼즈 작동!)
        if (currentScene == mainSceneName) return;

        // 옵션창이 켜져 있으면 -> 옵션 닫고 퍼즈창으로 복귀
        if (optionsManager?.optionsPanel != null && optionsManager.optionsPanel.activeSelf)
        {
            pauseManager.CloseOptions();
        }
        // 퍼즈 상태면 -> 게임 재개
        else if (pauseManager != null && pauseManager.isPaused)
        {
            pauseManager.ResumeGame();
        }
        // 기본 상태면 -> 퍼즈 실행
        else if (pauseManager != null)
        {
            pauseManager.PauseGame();
        }
    }

    // --- 외부 호출용 API ---
    public void RequestPause() => pauseManager.PauseGame();
    public void RequestOptions() => optionsManager.Open();
}