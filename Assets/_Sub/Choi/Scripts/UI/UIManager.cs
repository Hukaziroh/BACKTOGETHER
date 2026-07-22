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
        if (Keyboard.current == null) return;

        // 1. ESC 입력 처리
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            HandleEscapeInput();
        }

        // 2. 관전 키 입력 처리 (Q: 관전 종료 / Tab: 다음 타겟 순환 관전)
        if (spectatorSystem != null)
        {
            if (Keyboard.current.qKey.wasPressedThisFrame)
            {
                spectatorSystem.StopSpectating();
            }

            // ★ 1, 2, 3, 4 키를 제거하고 Tab 키를 누를 때마다 다음 타겟으로 순환 관전
            // (주의: 이모지 메뉴가 열려있지 않을 때만 관전 순환이 작동하도록 예외 처리를 추가했습니다)
            bool isEmojiMenuOpen = EmojiRadialMenu.Instance != null && EmojiRadialMenu.Instance.IsOpen();

            if (!isEmojiMenuOpen && Keyboard.current.tabKey.wasPressedThisFrame)
            {
                spectatorSystem.CycleNextTarget(); // SpectatorSystem에 이 이름의 함수가 있다고 가정합니다. (없다면 아래 참고)
            }
        }

        // 3. [관제탑 역할] T 키 입력 감지 후 이모티콘 패널(EmojiRadialMenu)에 신호 전달
        string currentScene = SceneManager.GetActiveScene().name;

        // 퍼즈 상태인지 확인
        bool isPaused = pauseManager != null && pauseManager.isPaused;

        // 메인 씬이 아니고, 퍼즈 상태가 아닐 때만 T 키 이모지 메뉴 작동
        if (currentScene != mainSceneName && !isPaused && EmojiRadialMenu.Instance != null)
        {
            // ★ Tab 키 대신 T 키를 누를 때 메뉴 토글 (열기/닫기)
            if (Keyboard.current.tKey.wasPressedThisFrame)
            {
                EmojiRadialMenu.Instance.ToggleMenu();
            }

            // 메뉴가 열려있는 동안 키보드 조작(A/D, 좌우 화살표, Enter 등) 업데이트 처리
            if (EmojiRadialMenu.Instance.IsOpen())
            {
                EmojiRadialMenu.Instance.OnMenuUpdate();
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