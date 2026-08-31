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
        if (!EmojiRadialMenu.IsEmojiAllowedInCurrentScene())
        {
            EmojiRadialMenu.Instance?.ForceClose();
            PlayerEmojiController.ForceHideAll();
        }

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

        // 설정(옵션/볼륨/키가이드) 관련 메뉴가 하나라도 열려 있다면 닫기 로직 수행
        if (optionsManager != null && optionsManager.IsSettingsOpen)
        {
            if (cancelPressed || pausePressed)
            {
                // 키 가이드의 ESC/B 전환은 PanelController 한 곳에서만 처리한다.
                // 여기서도 닫으면 진행 중인 트윈의 완료 콜백이 옵션을 다시 여는 경쟁 상태가 생긴다.
                if (optionsManager.IsAnyKeyGuideActive())
                {
                    inputCooldown = 0.2f;
                    return;
                }
                // 1. 볼륨 패널이 열려있으면 닫기
                else if (optionsManager.volumePanel != null && optionsManager.volumePanel.activeSelf)
                {
                    optionsManager.CloseVolumePanel();
                }
                // 2. 메인 옵션 패널이 열려있으면 닫기
                else if (optionsManager.optionsPanel != null && optionsManager.optionsPanel.activeSelf)
                {
                    optionsManager.Close();
                }

                inputCooldown = 0.2f;
            }
            return;
        }

        // --- 설정 메뉴가 모두 닫혀 있을 때만 퍼즈 기능 실행 ---
        if (scene == mainSceneName) return;

        if (pauseManager != null)
        {
            if (pauseManager.isPaused)
            {
                // 퍼즈 중일 때 ESC/Pause 누르면 해제
                if (pausePressed || cancelPressed)
                {
                    pauseManager.ResumeGame();
                    inputCooldown = 0.2f;
                }
            }
            else
            {
                // 게임 중일 때 Pause 키만 누르면 퍼즈 켜기
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
        if (!EmojiRadialMenu.IsEmojiAllowedInCurrentScene())
        {
            EmojiRadialMenu.Instance?.ForceClose();
            PlayerEmojiController.ForceHideAll();
        }

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

        bool isLobby = scene.name == lobbySceneName || scene.name == "NLobby";
        bool isChapter = scene.name != mainSceneName && !isLobby;
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

    public void RequestPause() => pauseManager?.PauseGame();
    public void RequestOptions() => optionsManager?.Open();
}
