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

            // 이모지 메뉴가 열려있지 않을 때만 관전 순환이 작동하도록 예외 처리
            bool isEmojiMenuOpen = EmojiRadialMenu.Instance != null && EmojiRadialMenu.Instance.IsOpen();

            if (!isEmojiMenuOpen && Keyboard.current.tabKey.wasPressedThisFrame)
            {
                spectatorSystem.CycleNextTarget();
            }
        }

        // 3. [관제탑 역할] T 키 입력 감지 후 이모티콘 패널(EmojiRadialMenu)에 홀드 방식 신호 전달
        string currentScene = SceneManager.GetActiveScene().name;

        // 퍼즈 상태인지 확인
        bool isPaused = pauseManager != null && pauseManager.isPaused;

        // 메인 씬이 아니고, 퍼즈 상태가 아닐 때만 T 키 이모지 메뉴 작동
        if (currentScene != mainSceneName && !isPaused && EmojiRadialMenu.Instance != null)
        {
            // T 키를 누르기 시작할 때 열고, 뗄 때 닫기 (홀드 방식)
            if (Keyboard.current.tKey.wasPressedThisFrame)
            {
                EmojiRadialMenu.Instance.OpenMenu();
            }
            else if (Keyboard.current.tKey.wasReleasedThisFrame)
            {
                EmojiRadialMenu.Instance.CloseMenu();
            }

            // 메뉴가 열려있는 동안 키보드 조작(A/D, 좌우 화살표 등 연속 이동) 업데이트 처리
            if (EmojiRadialMenu.Instance.IsOpen())
            {
                EmojiRadialMenu.Instance.OnMenuUpdate();
            }
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // ★ 씬이 넘어갈 때 옵션 패널이나 퍼즈 패널이 켜져있다면 강제로 닫고 게임 상태 정상화
        if (optionsManager != null && optionsManager.optionsPanel != null && optionsManager.optionsPanel.activeSelf)
        {
            pauseManager?.CloseOptions();
        }

        if (pauseManager != null && pauseManager.isPaused)
        {
            pauseManager.ResumeGame();
        }

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

        // T 키를 누르고 있거나 이모지 메뉴가 열려있는 상태라면 ESC 입력 무시 (패널창 안 나옴)
        if ((Keyboard.current != null && Keyboard.current.tKey.isPressed) ||
            (EmojiRadialMenu.Instance != null && EmojiRadialMenu.Instance.IsOpen()))
        {
            return;
        }

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