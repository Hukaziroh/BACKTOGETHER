using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;
    private PlayerControls.PlayerControls controls; 

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

            controls = new PlayerControls.PlayerControls(); 
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;

        if (controls != null)
        {
            controls.GamePlay.Back.performed += OnBackPressed;
            controls.GamePlay.Enable();
        }
    }
    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (controls != null)
        {
            controls.GamePlay.Back.performed -= OnBackPressed;
            controls.GamePlay.Disable();
        }
    }

    private void OnBackPressed(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed)
            return;


        // 옵션 열려있으면 옵션 닫기
        if (optionsManager != null &&
            optionsManager.optionsPanel != null &&
            optionsManager.optionsPanel.activeSelf)
        {
            optionsManager.Close();
            return;
        }


        HandleEscapeInput();
    }
    private void Update()
    {
        if (Keyboard.current != null)
        {
            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                HandleEscapeInput();
            }
        }


        if (spectatorSystem != null && Keyboard.current != null)
        {
            if (Keyboard.current.qKey.wasPressedThisFrame)
            {
                spectatorSystem.StopSpectating();
            }

            bool isEmojiMenuOpen = EmojiRadialMenu.Instance != null && EmojiRadialMenu.Instance.IsOpen();

            if (!isEmojiMenuOpen && Keyboard.current.tabKey.wasPressedThisFrame)
            {
                spectatorSystem.CycleNextTarget();
            }
        }

        string currentScene = SceneManager.GetActiveScene().name;
        bool isPaused = pauseManager != null && pauseManager.isPaused;

        if (currentScene != mainSceneName &&
            !isPaused &&
            EmojiRadialMenu.Instance != null &&
            Keyboard.current != null)
        {
            if (Keyboard.current.tKey.wasPressedThisFrame)
            {
                EmojiRadialMenu.Instance.OpenMenu();
            }
            else if (Keyboard.current.tKey.wasReleasedThisFrame)
            {
                EmojiRadialMenu.Instance.CloseMenu();
            }

            if (EmojiRadialMenu.Instance.IsOpen())
            {
                EmojiRadialMenu.Instance.OnMenuUpdate();
            }
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (optionsManager != null && optionsManager.optionsPanel != null && optionsManager.optionsPanel.activeSelf)
        {
            if (scene.name == mainSceneName)
            {
                optionsManager.Close();
            }
            else
            {
                pauseManager?.CloseOptions();
            }
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
        string scene = SceneManager.GetActiveScene().name;

        if (scene == lobbySceneName)
        {
            return;
        }

        if (pauseManager != null)
        {
            if (pauseManager.isPaused)
                pauseManager.ResumeGame();
            else
                pauseManager.PauseGame();
        }
    }

    public void RequestPause() => pauseManager.PauseGame();
    public void RequestOptions() => optionsManager.Open();
}