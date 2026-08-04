using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using TMPro;
using Mirror;
using Epic.OnlineServices;
using Epic.OnlineServices.Lobby;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class OptionsManager : MonoBehaviour
{
    public static OptionsManager instance;

    [Header("UI Reference")]
    public GameObject optionsPanel;

    [Header("Settings")]
    public AudioMixer audioMixer;
    public Slider volumeSlider;
    public Toggle fullscreenToggle;

    [Header("방 코드 UI (모든 플레이어 공용)")]
    public GameObject roomCodeUIContainer;

    [Header("★ 다국어 영향 안 받는 완전 독립형 TMPro")]
    public TextMeshProUGUI independentRoomCodeText;

    public Button toggleVisibilityButton;

    [Header("Key Guide Panel")]
    public GameObject keyGuidePanel;
    public Button keyGuideToggleButton;

    public bool IsCodeVisible => isCodeVisible;

    private bool isCodeVisible = true;
    private Coroutine fetchCodeRoutine;

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

    void Start()
    {
        if (fullscreenToggle != null)
        {
            fullscreenToggle.isOn = Screen.fullScreen;
            fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
        }

        if (volumeSlider != null)
        {
            volumeSlider.onValueChanged.AddListener(SetVolume);
        }

        if (toggleVisibilityButton != null)
        {
            toggleVisibilityButton.onClick.AddListener(ToggleRoomCodeVisibility);
        }

        if (keyGuideToggleButton != null)
        {
            keyGuideToggleButton.onClick.AddListener(ToggleKeyGuide);
        }
    }

    void Update()
    {
        if (keyGuidePanel != null && keyGuidePanel.activeSelf)
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                ToggleKeyGuide();
            }
        }
    }

    public void Open()
    {
        if (optionsPanel != null) optionsPanel.SetActive(true);
        if (keyGuidePanel != null) keyGuidePanel.SetActive(false);
        if (PauseManager.instance != null) PauseManager.instance.ClosePause();

        UpdateRoomCodeUI();

        if (string.IsNullOrEmpty(PrivateLobbyManager.currentShortCode))
        {
            if (fetchCodeRoutine != null) StopCoroutine(fetchCodeRoutine);
            fetchCodeRoutine = StartCoroutine(FetchRoomCodeAsync());
        }

        if (GlobalSceneInputManager.Instance != null && optionsPanel != null)
        {
            GlobalSceneInputManager.Instance.SetFocusScope(optionsPanel);
        }
    }

    public void Close()
    {
        if (fetchCodeRoutine != null)
        {
            StopCoroutine(fetchCodeRoutine);
            fetchCodeRoutine = null;
        }

        if (optionsPanel != null) optionsPanel.SetActive(false);
        if (keyGuidePanel != null) keyGuidePanel.SetActive(false);

        bool isMainScene = SceneManager.GetActiveScene().name == "Main";

        if (!isMainScene && PauseManager.instance != null && PauseManager.instance.pausePanel != null)
        {
            PauseManager.instance.pausePanel.SetActive(true);
            if (GlobalSceneInputManager.Instance != null)
            {
                GlobalSceneInputManager.Instance.SetFocusScope(PauseManager.instance.pausePanel);
            }
        }
        else
        {
            if (GlobalSceneInputManager.Instance != null)
            {
                GlobalSceneInputManager.Instance.ClearFocusScope();
            }
        }
    }

    private void ToggleRoomCodeVisibility()
    {
        isCodeVisible = !isCodeVisible;
        UpdateRoomCodeUI();

        if (LobbySyncManager.instance != null)
        {
            LobbySyncManager.instance.RefreshRoomCodeUIState();
        }
    }

    public void ToggleKeyGuide()
    {
        if (keyGuidePanel != null)
        {
            bool isKeyGuideActive = keyGuidePanel.activeSelf;

            if (!isKeyGuideActive)
            {
                keyGuidePanel.SetActive(true);
                if (optionsPanel != null) optionsPanel.SetActive(false);

                if (GlobalSceneInputManager.Instance != null)
                {
                    GlobalSceneInputManager.Instance.SetFocusScope(keyGuidePanel);
                }
            }
            else
            {
                keyGuidePanel.SetActive(false);
                if (optionsPanel != null)
                {
                    optionsPanel.SetActive(true);
                    UpdateRoomCodeUI();

                    if (GlobalSceneInputManager.Instance != null)
                    {
                        GlobalSceneInputManager.Instance.SetFocusScope(optionsPanel);
                    }
                }
            }
        }
    }

    private IEnumerator FetchRoomCodeAsync()
    {
        float timeout = 3.0f;
        while (timeout > 0f)
        {
            string code = GetCodeFromEOS();
            if (!string.IsNullOrEmpty(code))
            {
                PrivateLobbyManager.currentShortCode = code;
                UpdateRoomCodeUI();
                yield break;
            }

            timeout -= 0.2f;
            yield return new WaitForSecondsRealtime(0.2f);
        }
    }

    private string GetCodeFromEOS()
    {
        if (NetworkManager.singleton == null) return null;

        EOSLobby eosLobby = NetworkManager.singleton.GetComponent<EOSLobby>();
        if (eosLobby != null && eosLobby.ConnectedToLobby && eosLobby.ConnectedLobbyDetails != null)
        {
            try
            {
                Attribute shortCodeAttribute = new Attribute();
                Result result = eosLobby.ConnectedLobbyDetails.CopyAttributeByKey(
                    new LobbyDetailsCopyAttributeByKeyOptions { AttrKey = "SHORTCODE" },
                    out shortCodeAttribute
                );

                if (result == Result.Success)
                {
                    return shortCodeAttribute.Data.Value.AsUtf8;
                }
            }
            catch
            {
            }
        }
        return null;
    }

    public void UpdateRoomCodeUI()
    {
        if (independentRoomCodeText == null) return;

        string displayCode = "";

        bool isMainScene = SceneManager.GetActiveScene().name == "Main";
        if (!isMainScene)
        {
            if (LobbySyncManager.instance != null)
            {
                displayCode = LobbySyncManager.instance.roomCode;
            }

            if (string.IsNullOrEmpty(displayCode))
            {
                displayCode = PrivateLobbyManager.currentShortCode;
            }
        }

        // 다국어 스크립트가 아예 붙어있지 않은 독립형 TMPro이므로 
        // 오직 코드 값이나 Empty, 마스킹 처리만 깔끔하게 직관적으로 꽂아넣습니다.
        if (isMainScene || string.IsNullOrEmpty(displayCode))
        {
            independentRoomCodeText.text = isCodeVisible ? "Empty" : "******";
        }
        else
        {
            independentRoomCodeText.text = isCodeVisible ? displayCode : "******";
        }

        if (LobbySyncManager.instance != null)
        {
            LobbySyncManager.instance.RefreshRoomCodeUIState();
        }
    }

    public void SetVolume(float volume)
    {
        if (audioMixer != null)
        {
            float db = Mathf.Log10(Mathf.Max(volume, 0.0001f)) * 20;
            audioMixer.SetFloat("Volume", db);
        }
    }

    public void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;

        if (!isFullscreen)
        {
            Screen.SetResolution(1280, 720, false);
        }
    }
}