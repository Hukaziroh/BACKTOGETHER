using Epic.OnlineServices;
using Epic.OnlineServices.Lobby;
using Mirror;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
// ❌ using UnityEngine.InputSystem; 삭제 완료!

public class OptionsManager : MonoBehaviour
{
    public static OptionsManager instance;

    // ❌ private PlayerControls.PlayerControls controls; 삭제 완료!

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

            // 플레이어 사운드(점프/피격/발소리/버튼 등)가 이 볼륨 슬라이더의 영향을 받도록
            // Master 그룹을 PlayerSoundUtility에 등록해둔다.
            if (audioMixer != null)
            {
                AudioMixerGroup[] groups = audioMixer.FindMatchingGroups("Master");
                if (groups.Length > 0) PlayerSoundUtility.DefaultSfxGroup = groups[0];
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // ❌ OnEnable, OnDisable, OnBack, Update 함수 모조리 삭제 완료!

    void Start()
    {
        if (fullscreenToggle != null)
        {
            fullscreenToggle.isOn = Screen.fullScreen;
            fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
        }

        if (volumeSlider != null)
        {
            // 슬라이더 초기 위치를 현재 실제 믹서 볼륨값에 맞춰 동기화 (안 하면 실제 소리는 안 작은데 바만 0에 있는 것처럼 보임)
            if (audioMixer != null && audioMixer.GetFloat("Volume", out float currentDb))
            {
                float currentVolume = Mathf.Pow(10f, currentDb / 20f);
                volumeSlider.SetValueWithoutNotify(currentVolume);
            }

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

    public void Open()
    {
        if (optionsPanel != null) optionsPanel.SetActive(true);
        if (keyGuidePanel != null) keyGuidePanel.SetActive(false);

        // 🌟 [핵심 수정] ClosePause()를 호출해서 퍼즈를 풀어버리지 않고, 퍼즈 패널만 잠시 숨김!
        if (PauseManager.instance != null && PauseManager.instance.pausePanel != null)
        {
            PauseManager.instance.pausePanel.SetActive(false);
        }

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

        if (optionsPanel != null)
            optionsPanel.SetActive(false);

        if (keyGuidePanel != null)
            keyGuidePanel.SetActive(false);

        bool isMainScene = SceneManager.GetActiveScene().name == "Main";

        if (!isMainScene && PauseManager.instance != null)
        {
            PauseManager.instance.isPaused = true;

            if (PauseManager.instance.pausePanel != null)
            {
                PauseManager.instance.pausePanel.SetActive(true);

                if (GlobalSceneInputManager.Instance != null)
                {
                    GlobalSceneInputManager.Instance.SetFocusScope(
                        PauseManager.instance.pausePanel
                    );
                }

                Button firstButton = PauseManager.instance.pausePanel.GetComponentInChildren<Button>();
                if (firstButton != null && EventSystem.current != null)
                {
                    EventSystem.current.SetSelectedGameObject(firstButton.gameObject);
                }
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

                    Button firstButton = optionsPanel.GetComponentInChildren<Button>();
                    if (firstButton != null && EventSystem.current != null)
                    {
                        EventSystem.current.SetSelectedGameObject(firstButton.gameObject);
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