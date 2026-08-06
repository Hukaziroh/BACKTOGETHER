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

    private float inputCooldown = 0f;

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
        if (controls != null) controls.GamePlay.Enable();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (controls != null) controls.GamePlay.Disable();
    }

    private void Update()
    {
      
        if (inputCooldown > 0f) inputCooldown -= Time.unscaledDeltaTime;

        if (controls == null) return;

        if (inputCooldown <= 0f)
        {
            bool pausePressed = controls.GamePlay.Pause.WasPressedThisFrame();
            bool backPressed = controls.GamePlay.Back.WasPressedThisFrame();

            if (pausePressed || backPressed)
            {
                HandleMenuInput(pausePressed, backPressed);
            }
        }

        if (spectatorSystem != null)
        {
            if (controls.GamePlay.ReturnToMe.WasPressedThisFrame())
            {
                spectatorSystem.StopSpectating();
            }

            bool isEmojiMenuOpen = EmojiRadialMenu.Instance != null && EmojiRadialMenu.Instance.IsOpen();
            if (!isEmojiMenuOpen && controls.GamePlay.SpectateNext.WasPressedThisFrame())
            {
                spectatorSystem.CycleNextTarget();
            }


            if (EmojiRadialMenu.Instance != null && EmojiRadialMenu.Instance.IsOpen())
            {
                EmojiRadialMenu.Instance.OnMenuUpdate();
            }
        }
    }

    private void HandleMenuInput(bool pausePressed, bool cancelPressed)
    {
        string scene = SceneManager.GetActiveScene().name;

        if (optionsManager != null &&
            optionsManager.keyGuidePanel != null &&
            optionsManager.keyGuidePanel.activeSelf)
        {
            if (cancelPressed || pausePressed) 
            {
                optionsManager.ToggleKeyGuide(); 
                inputCooldown = 0.2f;
            }
            return;
        }

        if (optionsManager != null)
        {
            bool isOptionsOpen = optionsManager.optionsPanel != null && optionsManager.optionsPanel.activeSelf;
            if (isOptionsOpen)
            {
                optionsManager.Close(); 
                inputCooldown = 0.2f;
                return;
            }
        }

        if (scene == mainSceneName) return;

        if (pauseManager != null)
        {
            if (pauseManager.isPaused)
            {
                pauseManager.ResumeGame();
                inputCooldown = 0.2f;
            }
            else
            {
                if (pausePressed)
                {
                    pauseManager.PauseGame();
                    inputCooldown = 0.2f;
                }
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

    public void RequestPause() => pauseManager.PauseGame();
    public void RequestOptions() => optionsManager.Open();
}