using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using System.Collections;

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
        }

        // 3. [관제탑 역할] 탭(Tab) 키 입력 감지 후 이모티콘 패널(EmojiRadialMenu)에 신호 전달
        string currentScene = SceneManager.GetActiveScene().name;

        // 메인 씬이 아닐 때만 탭 키 이모지 메뉴 작동
        if (currentScene != mainSceneName && Keyboard.current != null && EmojiRadialMenu.Instance != null)
        {
            // 누르기 시작할 때 (오픈 신호)
            if (Keyboard.current.tabKey.wasPressedThisFrame)
            {
                EmojiRadialMenu.Instance.OpenMenu();
            }
            // 꾹 누르고 있는 동안 (마우스 방향 계산 신호)
            if (Keyboard.current.tabKey.isPressed)
            {
                EmojiRadialMenu.Instance.OnMenuStay();
            }
            // 뗄 때 (선택 및 닫기 신호)
            if (Keyboard.current.tabKey.wasReleasedThisFrame)
            {
                EmojiRadialMenu.Instance.CloseMenu();
            }
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        bool isChapter = (scene.name != mainSceneName && scene.name != lobbySceneName);
        progressTrackerUI?.SetActive(isChapter);

        GameObject mainCam = GameObject.FindGameObjectWithTag("MainCamera");
        if (mainCam != null)
        {
            if (mainCam.GetComponent<SpectatorCamera>() == null)
            {
                mainCam.AddComponent<SpectatorCamera>();
            }
        }
    }

    private void HandleEscapeInput()
    {
        string currentScene = SceneManager.GetActiveScene().name;

        if (currentScene == mainSceneName) return;

        if (optionsManager?.optionsPanel != null && optionsManager.optionsPanel.activeSelf)
        {
            pauseManager.CloseOptions();
        }
        else if (pauseManager != null && pauseManager.isPaused)
        {
            pauseManager.ResumeGame();
        }
        else if (pauseManager != null)
        {
            pauseManager.PauseGame();
        }
    }

    public void RequestPause() => pauseManager.PauseGame();
    public void RequestOptions() => optionsManager.Open();
}