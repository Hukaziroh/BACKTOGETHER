using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using System.Linq; // 💡 배열 처리를 쉽게 하기 위해 추가

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("기능 매니저 연결")]
    [SerializeField] private PauseManager pauseManager;
    [SerializeField] private OptionsManager optionsManager;

    [Header("UI 연결")]
    // 변수 이름을 목적에 맞게 변경했습니다 (progressBarUI -> progressTrackerUI)
    [SerializeField] private GameObject progressTrackerUI;

    [Header("관전 연결")]
    [SerializeField] private SpectatorSystem spectatorSystem;

    [Header("설정")]
    [SerializeField] private string[] excludedScenes = { "Main", "Lobby" };

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
        // 1. ESC 입력 처리 (이미 잘 작성하신 부분)
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            HandleEscapeInput();
        }

        // 2. 관전 키 입력 처리 (최신 Input System 방식으로 수정)
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
        // 💡 씬 포함 여부를 한 줄로 간단하게 체크
        bool isChapter = !excludedScenes.Contains(scene.name);

        // 💡 null 체크를 간결하게 (progressTrackerUI가 할당 안 되어도 에러 방지)
        progressTrackerUI?.SetActive(isChapter);

        // [추가] 관전 카메라 자동 연결 로직
        GameObject mainCam = GameObject.FindGameObjectWithTag("MainCamera");
        if (mainCam != null)
        {
            // 만약 카메라에 SpectatorCamera 스크립트가 없다면?
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

        // 메인/로비 씬에서는 퍼즈 작동 안 함
        if (excludedScenes.Contains(currentScene)) return;

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